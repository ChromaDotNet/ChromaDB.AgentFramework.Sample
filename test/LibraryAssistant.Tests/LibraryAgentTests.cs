namespace LibraryAssistant.Tests;

public sealed class LibraryAgentTests(ChromaFixture chroma) : IClassFixture<ChromaFixture>
{
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stores_the_handbook_in_Chroma()
    {
        using var library = await CreateAgentAsync(new RecordingChatClient(), NewReader());

        var handbook = await chroma.ChromaClient.GetCollection(LibraryAgent.HandbookCollectionName, cancellationToken: _cancellationToken);
        var collection = chroma.ChromaClient.GetCollectionClient(handbook);
        var pets = await collection.Get("pets", cancellationToken: _cancellationToken);

        Assert.Equal(Handbook.Notes.Count, await collection.Count(_cancellationToken));
        Assert.NotNull(pets);
        Assert.Equal(Handbook.Notes.Single(note => note.Id == "pets").Text, pets.Metadata!["Text"]);
    }

    [Fact]
    public async Task Gives_the_model_the_handbook_passage_that_answers_the_question()
    {
        var model = new RecordingChatClient();
        using var library = await CreateAgentAsync(model, NewReader());

        await AskAsync(library, "Can dogs enter the library?");

        Assert.Contains("Only assistance dogs may enter the library.", model.LastMessagesText);
    }

    [Fact]
    public async Task Remembers_what_the_reader_said_in_an_earlier_session()
    {
        var model = new RecordingChatClient();
        using var library = await CreateAgentAsync(model, NewReader());

        await AskAsync(library, "Hi, I'm Ada. I love science fiction novels.");
        await AskAsync(library, "Which novels do I love?");

        Assert.Contains("I love science fiction novels.", model.LastMessagesText);
    }

    [Fact]
    public async Task Does_not_share_the_memories_of_a_reader_with_another_reader()
    {
        var model = new RecordingChatClient();
        using var ada = await CreateAgentAsync(model, NewReader());
        using var bob = await CreateAgentAsync(model, NewReader());

        await AskAsync(ada, "My favourite colour is teal.");

        await AskAsync(bob, "What is my favourite colour?");
        var bobSees = model.LastMessagesText;

        await AskAsync(ada, "What is my favourite colour?");
        var adaSees = model.LastMessagesText;

        Assert.DoesNotContain("teal", bobSees);
        Assert.Contains("My favourite colour is teal.", adaSees);
    }

    private Task<LibraryAgent> CreateAgentAsync(RecordingChatClient model, string readerId)
        => LibraryAgent.CreateAsync(model, chroma.VectorStore, ChromaFixture.EmbeddingDimensions, readerId, _cancellationToken);

    // Each question goes in a new session, with no chat history: anything from earlier questions comes from Chroma.
    private async Task AskAsync(LibraryAgent library, string question)
        => await library.Agent.RunAsync(question, await library.Agent.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);

    // Each test uses readers of its own, so the memories of one test do not reach another.
    private static string NewReader() => Guid.NewGuid().ToString("N");
}
