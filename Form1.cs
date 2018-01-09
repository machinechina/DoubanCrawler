using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using CsQuery;
using CsQuery.Utility;

namespace DoubanCrawler
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 目前直接页面爬虫
        /// 之后要改为使用豆瓣API
        /// https://developers.douban.com/wiki/?title=book_v2
        /// </summary>
        String[] _commonTags = { "电脑", "健康", "学习", "文学", "科技", "--" };
        public Form1()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
        }

        private void BtnFind_Click(object sender, EventArgs e)
        {
            var fileNames = Directory.GetFiles(txtPath.Text, "*", SearchOption.AllDirectories).Select(f => new { FilePath = Path.GetDirectoryName(f).Replace(txtPath.Text, ""), FileName = Path.GetFileNameWithoutExtension(f), FullPath = f }).ToArray();
            dataGridView1.DataSource = fileNames;
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            (dataGridView1.DataSource as IEnumerable<dynamic>).Take(100).Select((d, index) => new { FileName = d.FileName, Index = index }).AsParallel().ForAll(row =>
               GetContent(row.FileName, row.Index));

        }

        private void ChangeRow(int rowIndex, dynamic content)
        {
            dataGridView1.Rows[rowIndex].Cells["BookName"].Value = content.BookName;
            dataGridView1.Rows[rowIndex].Cells["BookName"].Style.ForeColor = content.IsMatched ? Color.Green : Color.Red;
            dataGridView1.Rows[rowIndex].Cells["Rating"].Value = content.Rating;
            dataGridView1.Rows[rowIndex].Cells["Tags"].Value = content.Tags;
        }

        private void GetContent(String fileName, int index)
        {
            try
            {
                dynamic content = QueryBook2(fileName);

                if (dataGridView1.InvokeRequired)
                {
                    BeginInvoke(new Action<int, dynamic>(ChangeRow), new[] { index, content });
                }
                else
                {
                    ChangeRow(index, content);
                }
            }
            catch (Exception)
            {
            }
        }

        private dynamic QueryBook1(string fileName)
        {
            var url = "https://www.douban.com/search?q=" + fileName;
            CQ dom = CQ.CreateFromUrl(url).Render();
            var span_book = dom["span"].FirstOrDefault(s => s.TextContent == "[书籍]");
            String bookName = "", rating = "", detailUrl = "", tags = "";
            try
            {
                bookName = span_book.NextElementSibling.InnerText;
                detailUrl = span_book.NextElementSibling.Attributes["href"];
            }
            catch { }

            try
            {
                rating = span_book.ParentNode.NextElementSibling.ChildElements.First(a => a.ClassName == "rating_nums").TextContent;
            }
            catch { }

            if (!String.IsNullOrEmpty(detailUrl))
            {
                //detail page
                CQ domDetail = CQ.CreateFromUrl(detailUrl).Render();
                tags = String.Join(",", _commonTags.Union(domDetail[".  tag"].Contents().Select(c => c.ToString())));
            }

            dynamic content = new { BookName = bookName, Rating = rating, Tags = tags, IsMatched = bookName.Trim().Equals(fileName.Trim()) };
            return content;
        }

        private dynamic QueryBook2(string fileName)
        {
            var url = "https://api.douban.com/v2/book/search?q=" + fileName;
            var httpClient = new HttpClient();
            var response = httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            string content = response.Content.ReadAsStringAsync().Result;
            var result = JSON.ParseJSON(content) as dynamic;
            String bookName = "", rating = "", tags = "";
            if (result.count > 0)
            {
                bookName = result.books[0].title;
                rating = result.books[0].rating.average;
                tags = String.Join(",", _commonTags.Union((result.books[0].tags as IEnumerable<dynamic>).Select(t => t.title)));
            }

            return new { BookName = bookName, Rating = rating, Tags = tags, IsMatched = bookName.Trim().Equals(fileName.Trim()) };
        }

        private void btnMove_Click(object sender, EventArgs e)
        {
            Parallel.ForEach<DataGridViewRow>(dataGridView1.Rows.Cast<DataGridViewRow>(), row =>
            {
                var moveTo = row.Cells["MoveTo"].Value?.ToString();
                if (!String.IsNullOrEmpty(moveTo) && row.DefaultCellStyle.BackColor == Color.Green)
                {

                    var moveFrom = (row.DataBoundItem as dynamic).FullPath;
                    moveTo = Path.Combine(txtPath.Text, moveTo.TrimStart('\\'));

                    Directory.CreateDirectory(moveTo);
                    File.Move(moveFrom, Path.Combine(moveTo, Path.GetFileName(moveFrom)));
                }
            });

        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            Parallel.ForEach<DataGridViewRow>(dataGridView1.Rows.Cast<DataGridViewRow>(), row =>
            {

                if (row.DefaultCellStyle.BackColor == Color.Red)
                {
                    var moveFrom = (row.DataBoundItem as dynamic).FullPath;
                    Directory.CreateDirectory(txtRecyclePath.Text);
                    File.Move(moveFrom, Path.Combine(txtRecyclePath.Text, Path.GetFileName(moveFrom)));
                }
            });
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["Delete"].Index)
            {
                var row = dataGridView1.Rows[e.RowIndex];
                row.DefaultCellStyle.BackColor = Color.Red;
            }
            else if (e.ColumnIndex == dataGridView1.Columns["Refresh"].Index)
            {
                GetContent(dataGridView1.Rows[e.RowIndex].Cells["FileName"].Value.ToString(), e.RowIndex);
            }
            else if (e.ColumnIndex == dataGridView1.Columns["Move"].Index)
            {
                var row = dataGridView1.Rows[e.RowIndex];
                row.DefaultCellStyle.BackColor = Color.Green;
            }
            else if (e.ColumnIndex == dataGridView1.Columns["Cancel"].Index)
            {
                var row = dataGridView1.Rows[e.RowIndex];
                row.DefaultCellStyle.BackColor = Color.White;
            }
        }



        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            txtRecyclePath.Text = Path.Combine(txtPath.Text, "_Deleted");
        }



        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["Tags"].Index && e.RowIndex >= 0)
            {

                var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var left = 0;
                foreach (var tag in cell.FormattedValue.ToString().Split(','))
                {
                    var text = TextRenderer.MeasureText(tag, dataGridView1.Font);
                    left += text.Width;
                    if (left > e.Location.X)
                    {
                        //MessageBox.Show(tag);
                        dataGridView1.Rows[e.RowIndex].Cells["MoveTo"].Value += "\\" + tag;
                        break;
                    }
                }
            }


        }

        private void btnMarkDeleted_Click(object sender, EventArgs e)
        {
            Parallel.ForEach<DataGridViewRow>(dataGridView1.Rows.Cast<DataGridViewRow>(), row =>
            {
                if (!String.IsNullOrEmpty(row.Cells["Rating"].Value?.ToString())
                && Convert.ToSingle(row.Cells["Rating"].Value) < Convert.ToSingle(txtMinScore.Text)
                && Convert.ToSingle(row.Cells["Rating"].Value) != 0)
                {
                    row.DefaultCellStyle.BackColor = Color.Red;
                }
            });
        }
    }



}