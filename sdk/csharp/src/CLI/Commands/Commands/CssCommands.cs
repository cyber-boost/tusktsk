using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace TuskLang.CLI.Commands
{
    /// <summary>
    /// CSS commands for TuskLang CLI
    /// </summary>
    public static class CssCommands
    {
        public static void AddCommands(RootCommand rootCommand)
        {
            var cssCommand = new Command("css", "CSS operations")
            {
                new Command("expand", "Expand CSS shortcodes in file")
                {
                    new Argument<string>("input", "Input file"),
                    new Argument<string>("output", "Output file (optional)"),
                    Handler = CommandHandler.Create<string, string>(ExpandCss)
                },
                new Command("map", "Show all shortcode → property mappings")
                {
                    Handler = CommandHandler.Create(ShowCssMap)
                }
            };

            rootCommand.AddCommand(cssCommand);
        }

        private static async Task<int> ExpandCss(string input, string output)
        {
            try
            {
                if (!File.Exists(input))
                {
                    GlobalOptions.WriteError($"Input file not found: {input}");
                    return 3;
                }

                GlobalOptions.WriteProcessing($"Expanding CSS shortcodes: {input}");

                var content = await File.ReadAllTextAsync(input);
                var expanded = ExpandCssShortcodes(content);

                var outputFile = output ?? Path.ChangeExtension(input, ".expanded.css");
                await File.WriteAllTextAsync(outputFile, expanded);

                GlobalOptions.WriteSuccess($"CSS expanded: {input} → {outputFile}");
                return 0;
            }
            catch (Exception ex)
            {
                GlobalOptions.WriteError($"CSS expansion failed: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> ShowCssMap()
        {
            GlobalOptions.WriteLine("CSS Shortcode Mappings:");
            GlobalOptions.WriteLine("=======================");
            GlobalOptions.WriteLine("mh → max-height");
            GlobalOptions.WriteLine("mw → max-width");
            GlobalOptions.WriteLine("ph → padding-height");
            GlobalOptions.WriteLine("pw → padding-width");
            GlobalOptions.WriteLine("mh → margin-height");
            GlobalOptions.WriteLine("mw → margin-width");
            GlobalOptions.WriteLine("bg → background");
            GlobalOptions.WriteLine("fg → color");
            GlobalOptions.WriteLine("fs → font-size");
            GlobalOptions.WriteLine("fw → font-weight");
            GlobalOptions.WriteLine("ff → font-family");
            GlobalOptions.WriteLine("ta → text-align");
            GlobalOptions.WriteLine("td → text-decoration");
            GlobalOptions.WriteLine("tt → text-transform");
            GlobalOptions.WriteLine("ls → letter-spacing");
            GlobalOptions.WriteLine("lh → line-height");
            GlobalOptions.WriteLine("ws → white-space");
            GlobalOptions.WriteLine("ov → overflow");
            GlobalOptions.WriteLine("pos → position");
            GlobalOptions.WriteLine("top → top");
            GlobalOptions.WriteLine("right → right");
            GlobalOptions.WriteLine("bottom → bottom");
            GlobalOptions.WriteLine("left → left");
            GlobalOptions.WriteLine("z → z-index");
            GlobalOptions.WriteLine("d → display");
            GlobalOptions.WriteLine("v → visibility");
            GlobalOptions.WriteLine("op → opacity");
            GlobalOptions.WriteLine("cur → cursor");
            GlobalOptions.WriteLine("out → outline");
            GlobalOptions.WriteLine("br → border");
            GlobalOptions.WriteLine("brr → border-radius");
            GlobalOptions.WriteLine("bs → box-shadow");
            GlobalOptions.WriteLine("tr → transition");
            GlobalOptions.WriteLine("tf → transform");
            GlobalOptions.WriteLine("fl → float");
            GlobalOptions.WriteLine("cl → clear");
            GlobalOptions.WriteLine("va → vertical-align");
            GlobalOptions.WriteLine("wb → word-break");
            GlobalOptions.WriteLine("ww → word-wrap");
            return 0;
        }

        private static string ExpandCssShortcodes(string content)
        {
            var shortcodeMap = new Dictionary<string, string>
            {
                ["mh"] = "max-height",
                ["mw"] = "max-width",
                ["ph"] = "padding-height",
                ["pw"] = "padding-width",
                ["mh"] = "margin-height",
                ["mw"] = "margin-width",
                ["bg"] = "background",
                ["fg"] = "color",
                ["fs"] = "font-size",
                ["fw"] = "font-weight",
                ["ff"] = "font-family",
                ["ta"] = "text-align",
                ["td"] = "text-decoration",
                ["tt"] = "text-transform",
                ["ls"] = "letter-spacing",
                ["lh"] = "line-height",
                ["ws"] = "white-space",
                ["ov"] = "overflow",
                ["pos"] = "position",
                ["top"] = "top",
                ["right"] = "right",
                ["bottom"] = "bottom",
                ["left"] = "left",
                ["z"] = "z-index",
                ["d"] = "display",
                ["v"] = "visibility",
                ["op"] = "opacity",
                ["cur"] = "cursor",
                ["out"] = "outline",
                ["br"] = "border",
                ["brr"] = "border-radius",
                ["bs"] = "box-shadow",
                ["tr"] = "transition",
                ["tf"] = "transform",
                ["fl"] = "float",
                ["cl"] = "clear",
                ["va"] = "vertical-align",
                ["wb"] = "word-break",
                ["ww"] = "word-wrap"
            };

            var expanded = content;
            foreach (var mapping in shortcodeMap)
            {
                var pattern = $@"\b{mapping.Key}\s*:";
                var replacement = $"{mapping.Value}:";
                expanded = Regex.Replace(expanded, pattern, replacement, RegexOptions.IgnoreCase);
            }

            return expanded;
        }
    }
} 