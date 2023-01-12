using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using StarsectorTools.Libs.Utils;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using HKW.TomlParse;
using System.Threading;
using System.Diagnostics;

namespace StarsectorToolsExpansionDemo
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class Page1 : Page
    {
        ObservableCollection<ModShowInfo> allModsShowInfo = new();
        private class ModShowInfo
        {
            public string Name { get; set; } = null!;
            public string Author { get; set; } = null!;
            public string DirectoryPath { get; set; } = null!;
            public ContextMenu ContextMenu { get; set; } = null!;
        }
        public Page1()
        {
            InitializeComponent();

            // 获取所有模组并创建显示实例
            foreach (var info in ModsInfo.AllModsInfo.Values)
            {
                allModsShowInfo.Add(new()
                {
                    Name = info.Name,
                    Author = info.Author,
                    DirectoryPath = info.DirectoryPath,
                    // 右键菜单
                    ContextMenu = CreateContextMenu(info),
                }); ;
            }
            DataGrid_ModsShowList.ItemsSource = allModsShowInfo;
        }
        private ContextMenu CreateContextMenu(ModInfo info)
        {
            ContextMenu contextMenu = new();
            contextMenu.Tag = false;
            // 设置为被调用后在生成菜单,以提高性能
            contextMenu.Loaded += (s, e) =>
            {
                if (contextMenu.Tag is true)
                    return;
                contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
                // 打开模组文件夹
                MenuItem menuItem = new();
                menuItem.Header = "打开模组文件夹";
                menuItem.Click += (s, e) =>
                {
                    Utils.OpenLink(info.DirectoryPath);
                };
                contextMenu.Items.Add(menuItem);
                contextMenu.Tag = true;
            };
            return contextMenu;
        }

        private void TextBox_SearchMods_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text;
                if (string.IsNullOrEmpty(text))
                    DataGrid_ModsShowList.ItemsSource = allModsShowInfo;
                else
                    DataGrid_ModsShowList.ItemsSource = new ObservableCollection<ModShowInfo>(allModsShowInfo.Where(i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)));
            }
        }

        private void Button_OpenLog_Click(object sender, RoutedEventArgs e)
        {
            Utils.OpenLink(STLog.LogFile);
        }

        private void Button_ReadJson_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "读取Json文件,推荐使用mod_info.json",
                Filter = $"Json File|*.json"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fullName = openFileDialog.FileName;
                string fileName = Path.GetFileName(fullName);
                if (Utils.JsonParse(fullName) is not JsonNode jsonNode)
                    return;
                if (fileName == "mod_info.json")
                {
                    if (ModInfo.Parse(jsonNode) is ModInfo modInfo)
                        Utils.ShowMessageBox($"模组名称: {modInfo.Name}\n模组作者: {modInfo.Author}\n模组描述: {modInfo.Description}\n");
                }
                else
                    Utils.ShowMessageBox(jsonNode.ToUTF8String());
            }
        }

        private void Button_WriteJson_Click(object sender, RoutedEventArgs e)
        {
            if (ModsInfo.AllModsInfo.Count == 0)
            {
                Utils.ShowMessageBox("无法输出,你的游戏没有安装mod");
                return;
            }
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "导出Json文件,从游戏已安装的mod中选取第一个进行导出",
                Filter = $"Json File|*.json"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fullName = saveFileDialog.FileName;
                string fileName = Path.GetFileName(fullName);
                ModInfo modInfo = ModsInfo.AllModsInfo.First().Value;
                JsonArray jsonArray = new();

                if (modInfo.Dependencies is not null)
                {
                    foreach (var dependencie in modInfo.Dependencies)
                        jsonArray.Add(dependencie.Name);
                }

                JsonObject jsonObject = new()
                {
                    ["id"] = modInfo.Id,
                    ["name"] = modInfo.Name,
                    ["author"] = modInfo.Author,
                    ["version"] = modInfo.Version,
                    ["gameVersion"] = modInfo.GameVersion,
                    ["description"] = modInfo.Description,
                    ["dependencies"] = jsonArray,
                    ["modPlugin"] = modInfo.ModPlugin,
                };

                jsonObject.SaveTo(fullName);
            }
        }

        private void Button_ReadXml_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "读取Xml文件,推荐使用游戏存档descriptor.xml",
                Filter = $"Xml File|*.xml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fullName = openFileDialog.FileName;
                string fileName = Path.GetFileName(fullName);
                XElement xmlData = XElement.Load(fullName);
                if (fileName == "descriptor.xml")
                {
                    // 获取所有名为"spec"的子节点
                    var list1 = xmlData.Descendants("spec").ToList();
                    // 从获取的子节点中查找包含"id"节点的元素
                    var list2 = list1.Where(x => x.Element("id") != null).ToList();
                    // 获取所有"id"节点的值
                    var list3 = list2.Select(x => x.Element("id")?.Value).ToList();
                    // var list = xes.Descendants("spec").Where(x => x.Element("id") != null).Select(x => x.Element("id")?.Value);
                    // var list = from x in xmlData.Descendants("spec")
                    //            where x.Element("id") != null
                    //            select x.Element("id")?.Value;
                    Utils.ShowMessageBox($"此存档中加入的模组:\n{string.Join("\n", list3)}");
                }
                else
                {
                    Utils.ShowMessageBox(xmlData.ToString());
                }
            }
        }

        private void Button_WriteXml_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "导出Xml文件",
                Filter = $"Xml File|*.xml"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fullName = saveFileDialog.FileName;
                string fileName = Path.GetFileName(fullName);
                XElement contacts = new("Contacts",
                    new XElement("Contact",
                        new XElement("Name", "Patrick Hines"),
                        new XElement("Phone", "206-555-0144"),
                        new XElement("Address",
                            new XElement("Street1", "123 Main St"),
                            new XElement("City", "Mercer Island"),
                            new XElement("State", "WA"),
                            new XElement("测试中文", "你是一个一个一个")
                        )
                    )
                );
                // 属性名称不能为1开头
                contacts.SetAttributeValue("_114514", 1919810);
                contacts.Save(fullName);
            }
        }

        private void Button_ReadToml_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "读取Toml文件",
                Filter = $"Toml File|*.toml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fullName = openFileDialog.FileName;
                string fileName = Path.GetFileName(fullName);
                TomlTable toml = TOML.Parse(fullName);
                Utils.ShowMessageBox(toml.ToString());
            }
        }

        private void Button_WriteToml_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "导出Toml文件",
                Filter = $"Toml File|*.toml"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                string fullName = saveFileDialog.FileName;
                string fileName = Path.GetFileName(fullName);
                TomlTable toml = new()
                {
                    ["Game"] = new TomlTable()
                    {
                        ["Path"] = "中文测试",
                        ["ClearLogOnStart"] = false,
                    },

                    ["Extras"] = new TomlTable()
                    {
                        ["LogLevel"] = STLogLevel.INFO.ToString(),
                        ["Lang"] = Thread.CurrentThread.CurrentUICulture.Name,
                    },

                    ["???"] = new TomlArray()
                    {
                        114514,1919810
                    }
                };
                toml.SaveTo(fullName);
            }
        }

        private void Button_MessageBoxInfo_Click(object sender, RoutedEventArgs e)
        {
            Utils.ShowMessageBox("信息");
        }

        private void Button_MessageBoxQuestion_Click(object sender, RoutedEventArgs e)
        {
            Utils.ShowMessageBox("问题", STMessageBoxIcon.Question);
        }

        private void Button_MessageBoxWarning_Click(object sender, RoutedEventArgs e)
        {
            Utils.ShowMessageBox("警告", STMessageBoxIcon.Warning);
        }

        private void Button_MessageBoxError_Click(object sender, RoutedEventArgs e)
        {
            Utils.ShowMessageBox("错误", STMessageBoxIcon.Error);
        }

        private void Button_MessageBoxSuccess_Click(object sender, RoutedEventArgs e)
        {
            Utils.ShowMessageBox("完成", STMessageBoxIcon.Success);
        }

        private void Button_StartGame_Click(object sender, RoutedEventArgs e)
        {
            // 错误的启动方式
            //Utils.OpenLink(GameInfo.ExeFile);

            // 正确的启动方式
            /* SS载入数据的基础路径为游戏启动路径
             * 如果使用上面的启动方式,则游戏的启动路径就变为了软件根目录,自然读取不到游戏文件
             * 使用控制台将启动路径定位至游戏根目录即可
             */
            if (Utils.FileExists(GameInfo.ExeFile))
            {
                Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {GameInfo.GameDirectory}");
                    process.StandardInput.WriteLine($"starsector.exe");
                    process.Close();
                }
            }
        }
    }
}
