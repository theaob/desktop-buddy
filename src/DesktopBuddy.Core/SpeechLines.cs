namespace DesktopBuddy.Core;

/// <summary>What a buddy says in its speech bubble, in its own voice: a cat mrrps and purrs, a dog woofs and wags.</summary>
public sealed record SpeechLines(string[] Woken, string[] GrabbedAwake, string[] Ask, string[] Thanks, string[] Sulk)
{
    private static readonly SpeechLines Cat = new(
        Woken: new[] { "Mrrp?!", "Hey! I was napping.", "Five more minutes...", "Rude.", "Mmf. What?", "I was having a dream about tuna." },
        GrabbedAwake: new[] { "Hey! Put me down!", "Mrrrow?!", "Where are we going?" },
        Ask: new[] { "Belly scratches?", "Scritches, please!", "Rub my belly? Just a little?", "Prrt? Belly?" },
        Thanks: new[] { "Purrrrr...", "Mrrp! Thank you!", "Yes, right there!" },
        Sulk: new[] { "Hmph.", "Fine. Be busy.", "I didn't want scratches anyway." });

    private static readonly SpeechLines Dog = new(
        Woken: new[] { "Wuf?!", "Hey! I was napping.", "Five more minutes...", "Mmf. What?", "I was dreaming about squirrels." },
        GrabbedAwake: new[] { "Hey! Put me down!", "Wuf?!", "Where are we going? Walkies?" },
        Ask: new[] { "Belly rubs?", "Rub my belly? Please?", "Belly? Belly!", "Who wants to rub a belly?" },
        Thanks: new[] { "Best human ever!", "Yes, right there!", "*happy tail thumping*" },
        Sulk: new[] { "Hmph.", "Fine. Be busy.", "*sad puppy eyes*" });

    public static SpeechLines For(Species species) => species == Species.Dog ? Dog : Cat;
}
