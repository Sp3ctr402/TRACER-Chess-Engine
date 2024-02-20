using Raylib_cs;
using System;
using System.IO;
using System.Numerics;

namespace ChessChallenge.Application
{
    public static class MenuUI
    {
        public static void DrawButtons(ChallengeController controller)
        {
            Vector2 buttonPos = UIHelper.Scale(new Vector2(150, 210));
            Vector2 buttonPos1 = UIHelper.Scale(new Vector2(400, 210));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(240, 55));
            float spacing = buttonSize.Y * 1.2f;
            float breakSpacing = spacing * 0.6f;

            // Game Buttons
            if (NextButtonInRow("TRACER vs Human", ref buttonPos, spacing, buttonSize))
            {
                var whiteType = controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.TRACER : ChallengeController.PlayerType.Human;
                var blackType = !controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.TRACER : ChallengeController.PlayerType.Human;
                controller.StartNewGame(whiteType, blackType);
            }
            if (NextButtonInRow("TRACER vs TRACER", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.TRACER);
            }
            if (NextButtonInRow("TRACER vs EvilBot", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.EvilBot);
            }
            if (NextButtonInRow("TRACER vs V1", ref buttonPos1, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.TRACER_V1);
            }
            if (NextButtonInRow("TRACER vs V2", ref buttonPos1, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.TRACER_V2);
            }
            if (NextButtonInRow("TRACER vs V3", ref buttonPos1, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.TRACER_V3);
            }
            if (NextButtonInRow("TRACER vs V4", ref buttonPos1, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.TRACER_V4);
            }
            if (NextButtonInRow("TRACER vs V5", ref buttonPos1, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.TRACER, ChallengeController.PlayerType.TRACER_V5);
            }

            // Page buttons
            buttonPos.Y += breakSpacing;

            if (NextButtonInRow("Save Games", ref buttonPos, spacing, buttonSize))
            {
                string pgns = controller.AllPGNs;
                string directoryPath = Path.Combine(FileHelper.AppDataPath, "Games");
                Directory.CreateDirectory(directoryPath);
                string fileName = FileHelper.GetUniqueFileName(directoryPath, "games", ".txt");
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, pgns);
                ConsoleHelper.Log("Saved games to " + fullPath, false, ConsoleColor.Blue);
            }
            if (NextButtonInRow("Rules & Help", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://github.com/SebLague/Chess-Challenge");
            }
            if (NextButtonInRow("Documentation", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://seblague.github.io/chess-coding-challenge/documentation/");
            }
            if (NextButtonInRow("Submission Page", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://forms.gle/6jjj8jxNQ5Ln53ie6");
            }

            // Window and quit buttons
            buttonPos.Y += breakSpacing;

            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            string windowButtonName = isBigWindow ? "Smaller Window" : "Bigger Window";
            if (NextButtonInRow(windowButtonName, ref buttonPos, spacing, buttonSize))
            {
                Program.SetWindowSize(isBigWindow ? Settings.ScreenSizeSmall : Settings.ScreenSizeBig);
            }
            if (NextButtonInRow("Exit (ESC)", ref buttonPos, spacing, buttonSize))
            {
                Environment.Exit(0);
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y += spacingY;
                return pressed;
            }
        }
    }
}