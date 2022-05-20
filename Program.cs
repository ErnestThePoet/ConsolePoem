using System.Text.RegularExpressions;
using System.Text.Json;

namespace ConsolePoem
{
    class Program
    {
        static PoemDatabaseManager poemDatabaseManager=new PoemDatabaseManager();
        static void Main(string[] args)
        {
            poemDatabaseManager.ReadPoemDatabase();
            HandleInteractionLoop();
        }

        static void HandleInteractionLoop()
        {
            PoemPrinter.PrintSinglePoem(poemDatabaseManager.GetRandomPoem());

            Regex regex = new Regex(" +");

            while (true)
            {
                Console.WriteLine();
                Console.Write(StringResources.CommandLineStart);

                string? input = Console.ReadLine();

                Console.WriteLine();

                // Ctrl+z
                if (input == null)
                {
                    break;
                }
                else
                {
                    input = regex.Replace(input.Trim(), " ");
                    if (HandleInputLine(input))
                    {
                        break;
                    }
                }
            }
        }

        // returns whether the interaction loop should stop.
        static bool HandleInputLine(string inputLine)
        {
            if (inputLine==String.Empty)
            {
                PoemPrinter.PrintSinglePoem(poemDatabaseManager.GetRandomPoem());
            }
            else
            {
                // length>=1
                string[] commands = inputLine.Split(' ');

                switch (commands[0])
                {
                    case "exit":
                    case "quit":
                        return true;

                    case "stat":
                        Console.WriteLine(poemDatabaseManager.DatabaseReadStatus);
                        break;

                    case "help":
                        foreach(string line in StringResources.HelpMessageLines)
                        {
                            Console.WriteLine(line);
                        }
                        break;

                    case "a":
                        switch (commands.Length)
                        {
                            case 1:
                                Console.WriteLine(StringResources.CommandAFailPos1);
                                break;
                            case 2:
                                PoemPrinter.PrintSinglePoem(
                                    poemDatabaseManager.GetRandomPoemByAuthor(commands[1]));
                                break;
                            default:
                                if (commands[2] == "a")
                                {
                                    PoemPrinter.PrintAuthorPoemGroupList(
                                        poemDatabaseManager.GetAllPoemsByAuthor(commands[1]));
                                }
                                else
                                {
                                    Console.WriteLine(StringResources.CommandAFailPos2);
                                }
                                break;
                        }

                        break;

                    case "t":
                        switch (commands.Length)
                        {
                            case 1:
                                Console.WriteLine(StringResources.CommandTFailPos1);
                                break;
                            default:
                                PoemPrinter.PrintAuthorPoemGroupList(
                                    poemDatabaseManager.GetAllPoemsByTitle(commands[1]));
                                break;
                        }

                        break;

                    case "c":
                        switch (commands.Length)
                        {
                            case 1:
                                Console.WriteLine(StringResources.CommandCFailPos1);
                                break;
                            default:
                                PoemPrinter.PrintAuthorPoemGroupList(
                                    poemDatabaseManager.GetAllPoemsByContent(commands[1]));
                                break;
                        }

                        break;

                    default:
                        Console.WriteLine(StringResources.NoSuchCommand);
                        break;
                }
            }

            return false;
        }
    }

    class PoemDatabaseManager
    {
        List<Poem> poems;
        Random random;
        string databaseReadStatus = "";

        public PoemDatabaseManager()
        {
            poems = new List<Poem>();
            random = new Random();
        }

        public void ReadPoemDatabase()
        {
            string jsonFileListRawJson;
            try
            {
                jsonFileListRawJson = File.ReadAllText(
                    AppDomain.CurrentDomain.BaseDirectory + "poem_db_list.json");
            }
            catch
            {
                Console.WriteLine(StringResources.ErrorReadingJSONList);
                return;
            }

            string[]? jsonFileNameList;
            try
            {
                jsonFileNameList = JsonSerializer.Deserialize<string[]>(jsonFileListRawJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine(StringResources.ErrorDeserializingJSONList);
                Console.WriteLine(string.Format(StringResources.ErrorDetail, ex.Message));
                return;
            }

            if(jsonFileNameList == null)
            {
                Console.WriteLine(StringResources.ErrorJSONListNull);
                return;
            }

            int readDatabaseCount = 0;

            foreach (string jsonFileName in jsonFileNameList)
            {
                string currentPoemsRawJson;
                List<Poem>? currentPoems;

                try
                {
                    currentPoemsRawJson = File.ReadAllText(
                        AppDomain.CurrentDomain.BaseDirectory + jsonFileName);
                }
                catch
                {
                    Console.WriteLine(string.Format(StringResources.ErrorReadingJSONPoemDB, jsonFileName));
                    continue;
                }

                try
                {
                    currentPoems = JsonSerializer.Deserialize<List<Poem>>(currentPoemsRawJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(StringResources.ErrorDeserializingJSONPoemDB, jsonFileName);
                    Console.WriteLine(string.Format(StringResources.ErrorDetail, ex.Message));
                    continue;
                }

                if(currentPoems == null)
                {
                    Console.WriteLine(StringResources.ErrorJSONPoemDBNull, jsonFileName);
                    continue;
                }

                poems = poems.Concat(currentPoems).ToList();
                readDatabaseCount++;
            }

            databaseReadStatus = string.Format(
                StringResources.DatabaseReadStatus, 
                jsonFileNameList.Length, 
                readDatabaseCount, 
                poems.Count);
        }

        public string DatabaseReadStatus { get { return databaseReadStatus; } }

        public Poem? GetRandomPoem()
        {
            if (poems.Count > 0)
            {
                return poems[random.Next(poems.Count)];
            }
            else
            {
                return null;
            }
        }

        public Poem? GetRandomPoemByAuthor(string authorNamePartial)
        {
            return GetRandomPoemIf(poem=>poem.author.Contains(authorNamePartial));
        }

        public List<IGrouping<string,Poem>> GetAllPoemsByAuthor(string authorNamePartial)
        {
            return GetPoemsIf(poem => poem.author.Contains(authorNamePartial));
        }

        public List<IGrouping<string, Poem>> GetAllPoemsByTitle(string titlePartial)
        {
            return GetPoemsIf(poem => poem.title.Contains(titlePartial));
        }

        public List<IGrouping<string, Poem>> GetAllPoemsByContent(string contentPartial)
        {
            return GetPoemsIf(
                poem => (from paragraph in poem.paragraphs
                         where paragraph.Contains(contentPartial)
                         select paragraph)
                         .Count() > 0);
        }

        private Poem? GetRandomPoemIf(Predicate<Poem> predicate)
        {
            var targetPoems =
                from poem in poems
                where predicate(poem)
                select poem;

            if(targetPoems.Count() > 0)
            {
                return targetPoems.ElementAt(random.Next(targetPoems.Count()));
            }
            else
            {
                return null;
            }
        }

        private List<IGrouping<string,Poem>> GetPoemsIf(Predicate<Poem> predicate)
        {
            var targetPoemGroups=
                from poem in poems
                where predicate(poem)
                group poem by poem.author into poemAuthorGroup
                orderby poemAuthorGroup.Key
                select poemAuthorGroup;

            return targetPoemGroups.ToList();
        }
    }

    static class PoemPrinter
    {
        static public void PrintSinglePoem(Poem? poem)
        {
            if(poem == null)
            {
                Console.WriteLine(StringResources.NoPoemsToPrint);
                return;
            }

            Console.WriteLine();

            Console.WriteLine(poem.title);
            Console.WriteLine($"[{poem.dynasty}]{poem.author}");

            Console.WriteLine();

            foreach (string paragraph in poem.paragraphs)
            {
                Console.WriteLine(paragraph);
            }

            if (poem.notes.Count() > 0)
            {
                Console.WriteLine();
                Console.WriteLine(StringResources.NoteBegin);

                foreach (string note in poem.notes)
                {
                    Console.WriteLine(note);
                }
            }

            Console.WriteLine();
        }

        static public void PrintAuthorPoemGroupList(List<IGrouping<string,Poem>> poemGroupList)
        {
            if (poemGroupList.Count == 0)
            {
                Console.WriteLine(StringResources.NoPoemsToPrint);
                return;
            }

            poemGroupList.ForEach(group =>
            {
                foreach (Poem poem in group)
                {
                    PrintSinglePoem(poem);
                }
            });
        }
    }

    static class StringResources
    {
        public static readonly string CommandLineStart = 
            "寻诗>> ";
        public static readonly string NoteBegin = "【注】：";
        public static readonly string CommandAFailPos1 = 
            "寻诗口令始为'a'时，须接诗人姓名或其部分，君可以口令'help'一览其详";
        public static readonly string CommandAFailPos2 = 
            "此后寻诗口令须接'a'，君可以口令'help'一览其详";
        public static readonly string CommandTFailPos1 = 
            "寻诗口令始为't'时，须接诗题或其部分，君可以口令'help'一览其详";
        public static readonly string CommandCFailPos1 = 
            "寻诗口令始为'c'时，须接诗句或其部分，君可以口令'help'一览其详";

        public static readonly string NoSuchCommand = 
            "无此寻诗口令，君可以'help'一览众口令";

        public static readonly string ErrorReadingJSONList = 
            "【错误】读诗库之目录不得，请查poem_db_list.json";
        public static readonly string ErrorDeserializingJSONList = 
            "【错误】解诗库之目录不得，请查poem_db_list.json";
        public static readonly string ErrorJSONListNull = 
            "【错误】解诗库之目录止得虚空也，请查poem_db_list.json";
        public static readonly string ErrorReadingJSONPoemDB = 
            "【错误】读诗库诗卷不得，故弃之，遇于{0}也";
        public static readonly string ErrorDeserializingJSONPoemDB = 
            "【错误】解诗库诗卷不得，故弃之，遇于{0}也";
        public static readonly string ErrorJSONPoemDBNull = 
            "【错误】解诗库诗卷止得虚空也，故弃之，请查{0}";
        public static readonly string ErrorDetail = 
            "其详可览如是：{0}";

        public static readonly string DatabaseReadStatus = 
            "诗库凡{0}诗卷，{1}读毕，通计{2}诗也";

        public static readonly string NoPoemsToPrint = 
            "无可读之诗词";

        public static readonly string[] HelpMessageLines =
        {
            "进入诗词世界后，在一行中输入这些指令来寻诗:",
            "   <nothing>: 随机诗词；",
            "   a <AuthorNamePartial>",
            "       <nothing>: 随机一首诗人名字包含AuthorNamePartial的诗词；",
            "       a: 显示符合条件的所有作者的所有诗词；",
            "   t <TitlePartial>: 以诗人名字排序，显示所有题目包含TitlePartial的诗词；",
            "   c <ContentPartial>: 以诗人名字排序，显示所有内容包含ContentPartial的诗词；",
            "   stat: 显示诗词数据库信息；",
            "   help: 显示本帮助；",
            "   exit|quit|<Ctrl+Z>: 离开诗词世界。"
        };
    }

    class Poem
    {
        public string title { get; set; }
        public string author { get; set; }
        public string dynasty { get; set; }
        public string[] paragraphs { get; set; }
        public string[] notes { get; set; }

        public Poem()
        {
            title = "";
            author = "";
            dynasty = "";
            paragraphs = new string[] { };
            notes = new string[] { };
        }
    }
}