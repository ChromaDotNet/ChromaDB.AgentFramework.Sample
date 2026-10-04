namespace LibraryAssistant;

/// <summary>
/// The handbook of the Riverside Community Library, a library that exists only in this sample,
/// so the model can answer only from what it finds in Chroma.
/// </summary>
public static class Handbook
{
    public static IReadOnlyList<HandbookNote> Notes { get; } =
    [
        new() { Id = "opening-hours", Topic = "Opening hours", Text = "The Riverside Community Library is open Monday to Friday from 9:00 to 19:00 and on Saturday from 10:00 to 14:00. It is closed on Sunday." },
        new() { Id = "loans", Topic = "Loans", Text = "A reader can borrow up to 8 books at a time for 21 days. A loan can be renewed twice online, unless another reader has reserved the book." },
        new() { Id = "late-returns", Topic = "Late returns", Text = "A late book costs 20 cents a day, up to 5 euros per book. Fines are paid at the front desk or with the library card." },
        new() { Id = "pets", Topic = "Pets", Text = "Only assistance dogs may enter the library. Other dogs can wait at the leash hooks next to the entrance, where there is a water bowl." },
        new() { Id = "study-rooms", Topic = "Study rooms", Text = "Study rooms are booked online up to 7 days ahead, for groups of 2 to 6 people, for at most 3 hours a day." },
        new() { Id = "wifi", Topic = "Wi-Fi", Text = "The guest Wi-Fi network is called Riverside-Guest. Its password is printed on the back of the library card." },
        new() { Id = "printing", Topic = "Printing", Text = "Printing costs 10 cents a page in black and white and 50 cents a page in colour, paid with the library card." },
        new() { Id = "book-club", Topic = "Book club", Text = "The science fiction book club meets on the first Thursday of every month at 18:30 in room B. New members are welcome and do not need to book." },
        new() { Id = "story-time", Topic = "Story time", Text = "Story time for children aged 3 to 7 is every Wednesday at 16:00 in the children's corner." },
        new() { Id = "lost-card", Topic = "Lost card", Text = "A lost library card is replaced at the front desk for 2 euros. The old card stops working at once." },
    ];
}
