using System.Security.Cryptography;
using System.Text;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Builds memorable passphrases from a curated word list using a cryptographically secure RNG.
/// </summary>
public sealed class PassphraseGenerator : IPassphraseGenerator
{
    public int WordListSize => Words.Length;

    public string Generate(PassphraseOptions? options = null)
    {
        options ??= new PassphraseOptions();
        int wordCount = Math.Clamp(options.WordCount, 2, 12);
        var separator = options.Separator ?? "-";

        var parts = new List<string>(wordCount);
        for (int i = 0; i < wordCount; i++)
        {
            var word = Words[RandomNumberGenerator.GetInt32(Words.Length)];
            if (options.Capitalize)
                word = char.ToUpperInvariant(word[0]) + word[1..];
            parts.Add(word);
        }

        var phrase = string.Join(separator, parts);

        if (options.IncludeNumber)
            phrase += separator + RandomNumberGenerator.GetInt32(10, 100);

        return phrase;
    }

    // A curated list of short, easily-read, unambiguous words.
    private static readonly string[] Words =
    {
        "able","acid","acorn","actor","agile","album","alert","amber","anchor","angel","apple","arch",
        "arena","armor","arrow","atlas","aurora","autumn","azure","bacon","badge","baker","balsa","banjo",
        "barge","basil","beach","beacon","beam","bean","bear","beaver","bee","beetle","bell","berry",
        "birch","bird","bison","blaze","bloom","blue","board","boat","bold","bolt","bonus","book",
        "boost","boot","brave","bread","brick","bridge","brisk","bronze","brook","brush","bubble","buddy",
        "bunny","cabin","cable","cactus","camel","candle","canoe","canyon","cargo","carol","castle","cedar",
        "chalk","charm","cherry","chess","chief","chimp","cider","cliff","cloud","clover","coast","cobra",
        "cocoa","comet","compass","coral","cosmic","cotton","cove","coyote","crane","crater","cream","creek",
        "crisp","crow","crown","cube","daisy","dawn","deer","delta","denim","desert","dew","diamond",
        "diet","dingo","dolphin","domino","donut","dove","dragon","dream","drift","drum","dune","eagle",
        "earth","ember","emerald","engine","ester","ever","falcon","feather","fern","ficus","field","finch",
        "fjord","flame","flax","flint","float","flora","forest","fox","frost","galaxy","garden","gecko",
        "ginger","glacier","glade","glass","globe","glow","gold","granite","grape","grove","guava","hammer",
        "harbor","harvest","hawk","hazel","heron","hill","honey","horizon","ivory","jade","jaguar","jasmine",
        "jewel","jolly","jungle","kayak","kettle","kiwi","koala","lagoon","lake","lantern","laurel","leaf",
        "lemon","lily","linen","lion","lotus","lunar","lynx","magnet","mango","maple","marble","meadow",
        "melon","meteor","mint","mocha","monsoon","moon","moss","mountain","nebula","nest","noble","north",
        "oak","oasis","ocean","olive","onyx","opal","orbit","orchid","otter","owl","palm","panda",
        "parrot","peach","pearl","pebble","pepper","petal","phoenix","pine","pixel","planet","plum","polar",
        "pond","poppy","prairie","prism","pueblo","pulse","quartz","quest","quill","rabbit","radar","rain",
        "raven","reef","ridge","river","robin","rocket","rose","ruby","rust","saffron","sage","salmon",
        "sand","sapphire","scout","sea","sedge","sequoia","shadow","shell","shore","silk","silver","sky",
        "slate","sloth","snow","solar","spark","sphinx","spruce","squid","stag","star","steel","stone",
        "storm","stream","summit","sun","swan","tango","tea","teal","tiger","topaz","torch","tower",
        "trail","tulip","tundra","turtle","umber","valley","velvet","vine","violet","vortex","walnut","wave",
        "willow","wind","wolf","wonder","woods","yarn","zebra","zenith","zephyr","zinc"
    };
}
