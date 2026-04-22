using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace EQModeChangeSimulator
{
    public class PpidDrawingMapForm : Form
    {
        // ===============================
        // 内部数据结构
        // ===============================
        class Item
        {
            public string PPID { get; set; }
            public string Drawing { get; set; }
        }

        private const string ConfigFile = "ppid_drawing_map.json";

        private DataGridView dgv;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnSave;

        private BindingList<Item> items = new BindingList<Item>();

        // 上一次成功发送给 C++ 的 hash
        private static string _lastSentHash = string.Empty;

        public PpidDrawingMapForm()
        {
            Text = "PPID ↔ 图纸映射";
            Width = 520;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            InitUI();
            LoadFromFile();
        }

        // ===============================
        // UI
        // ===============================
        private void InitUI()
        {
            dgv = new DataGridView
            {
                Left = 10,
                Top = 10,
                Width = 480,
                Height = 300,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "PPID",
                DataPropertyName = "PPID",
                Width = 200
            });

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "图纸名称",
                DataPropertyName = "Drawing",
                Width = 250
            });

            dgv.DataSource = items;

            btnAdd = new Button
            {
                Text = "新增",
                Left = 10,
                Top = 320,
                Width = 80
            };
            btnAdd.Click += (s, e) => items.Add(new Item());

            btnDelete = new Button
            {
                Text = "删除",
                Left = 110,
                Top = 320,
                Width = 80
            };
            btnDelete.Click += (s, e) =>
            {
                if (dgv.CurrentRow?.DataBoundItem is Item it)
                    items.Remove(it);
            };

            btnSave = new Button
            {
                Text = "保存",
                Left = 210,
                Top = 320,
                Width = 100
            };
            btnSave.Click += (s, e) =>
            {
                SaveToFile();
                TrySendToCppIfChanged();
            };

            Controls.AddRange(new Control[]
            {
                dgv, btnAdd, btnDelete, btnSave
            });
        }

        // ===============================
        // 文件 IO
        // ===============================
        private void LoadFromFile()
        {
            if (!System.IO.File.Exists(ConfigFile))
                return;

            try
            {
                string json = System.IO.File.ReadAllText(ConfigFile, Encoding.UTF8);
                var list = JsonConvert.DeserializeObject<List<Item>>(json);

                if (list != null)
                {
                    items.Clear();
                    foreach (var it in list)
                        items.Add(it);
                }
            }
            catch { }
        }

        private void SaveToFile()
        {
            var list = GetCleanList();
            string json = JsonConvert.SerializeObject(list, Formatting.Indented);
            System.IO.File.WriteAllText(ConfigFile, json, Encoding.UTF8);
            MessageBox.Show("保存成功");
        }

        // ===============================
        // 对外静态方法（TCP 连上时用）
        // ===============================
        public static void TrySendLatestToCpp()
        {
            if (Form1.Instance == null)
                return;

            if (!System.IO.File.Exists(ConfigFile))
                return;

            try
            {
                string json = System.IO.File.ReadAllText(ConfigFile, Encoding.UTF8);
                var list = JsonConvert.DeserializeObject<List<Item>>(json);
                if (list == null) return;

                string hash = CalcHash(json);
                if (hash == _lastSentHash)
                    return;

                bool ok = Form1.Instance.SendToCpp(
                    "PpidDrawingMapUpdate",
                    new
                    {
                        count = list.Count,
                        maps = list.Select(x => new
                        {
                            ppid = x.PPID,
                            drawing = x.Drawing
                        }).ToList()
                    });

                if (ok)
                    _lastSentHash = hash;
            }
            catch { }
        }

        // ===============================
        // 保存后触发
        // ===============================
        private void TrySendToCppIfChanged()
        {
            var list = GetCleanList();
            string json = JsonConvert.SerializeObject(list);
            string hash = CalcHash(json);

            if (hash == _lastSentHash)
                return;

            bool ok = Form1.Instance?.SendToCpp(
                "PpidDrawingMapUpdate",
                new
                {
                    count = list.Count,
                    maps = list.Select(x => new
                    {
                        ppid = x.PPID,
                        drawing = x.Drawing
                    }).ToList()
                }) ?? false;

            if (ok)
                _lastSentHash = hash;
        }

        private List<Item> GetCleanList()
        {
            return items
                .Where(x => !string.IsNullOrWhiteSpace(x.PPID))
                .Select(x => new Item
                {
                    PPID = x.PPID.Trim(),
                    Drawing = x.Drawing?.Trim()
                })
                .ToList();
        }

        private static string CalcHash(string text)
        {
            using (var md5 = MD5.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                byte[] hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash);
            }
        }
    }
}
