using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImgtoCharMat
{
    public partial class Form1 : Form
    {
        private IEnumerator<Image> proc;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (proc == null)
                proc = GetProcess().GetEnumerator();
            if (proc.MoveNext())
                BackgroundImage = proc.Current;
            else
                proc = null;
        }

        private IEnumerable<Image> GetProcess()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK)
                yield break;
            using (Bitmap org = new Bitmap(ofd.FileName))
            {
                yield return org;
                const int UnitSize = 4;
                using (Bitmap scale = new Bitmap(org,
                    new Size(
                        (int)Math.Ceiling(org.Width / 15.0*4),
                        (int)Math.Ceiling(org.Height / 27.0*4))))
                {
                    yield return scale;
                    using (Bitmap scaleBack = new Bitmap(scale.Width * UnitSize, scale.Height * UnitSize))
                    using (Graphics g = Graphics.FromImage(scaleBack))
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(scale,
                            new Rectangle(0, 0, scaleBack.Width, scaleBack.Height),
                            new Rectangle(0, 0, scale.Width, scale.Height),
                            GraphicsUnit.Pixel);
                        yield return scaleBack;
                    }
                    List<KeyValuePair<float, char>> chmp = new List<KeyValuePair<float, char>>();
                    using (Font font = new Font("Consolas", 16))
                    {
                        int cw, ch;
                        using (Graphics g = Graphics.FromImage(org))
                        {
                            var s = g.MeasureString("M", font);
                            cw = (int)Math.Ceiling(s.Width);
                            ch = (int)Math.Ceiling(s.Height);
                        }
                        using (Bitmap charMap = new Bitmap((127 - 32) * cw, ch * 4))
                        using (Graphics g = Graphics.FromImage(charMap))
                        {
                            g.FillRectangle(Brushes.White, new Rectangle(0, 0, charMap.Width, charMap.Height));
                            for (int i = 32; i < 127; i++)
                            {
                                int col = (i - 32) % 32;
                                int row = (i - 32) / 32;
                                g.DrawString(((char)i).ToString(), font,
                                    Brushes.Black,
                                    new PointF(col * cw, row * ch));
                                int bl = 0;
                                for (int x = 0; x < cw; x++)
                                    for (int y = 0; y < ch; y++)
                                        bl += charMap.GetPixel(col * cw + x, row * ch + y).R;
                                float blr = 1 - bl / (cw * ch * 255.0f);
                                chmp.Add(new KeyValuePair<float, char>(blr, (char)i));
                            }
                            yield return charMap;
                        }
                        {
                            Bitmap charMap = new Bitmap((127 - 32) * cw, ch * 4);
                            using (Graphics g = Graphics.FromImage(charMap))
                            {
                                chmp.Sort((a, b) => Math.Sign(a.Key - b.Key));
                                g.FillRectangle(Brushes.White, new Rectangle(0, 0, charMap.Width, charMap.Height));
                                for (int i = 32; i < 127; i++)
                                {
                                    int col = (i - 32) % 32;
                                    int row = (i - 32) / 32;
                                    g.DrawString(chmp[i - 32].Value.ToString(), font,
                                        Brushes.Black,
                                        new PointF(col * cw, row * ch));
                                }
                                yield return charMap;
                            }
                        }
                    }
                    StringBuilder sb = new StringBuilder();
                    for (int y = 0; y < scale.Height; y++)
                    {
                        for (int x = 0; x < scale.Width; x++)
                        {
                            float b = scale.GetPixel(x, y).GetBrightness();
                            double inv = 3;
                            if (b > .5f)
                                b = (float)Math.Pow(2 * b - 1, 1 / inv) / 2 + .5f;
                            else
                                b = .5f - (float)Math.Pow(1 - 2 * b, 1 / inv) / 2;
                            sb.Append(chmp[(int)Math.Floor(b * (chmp.Count - 1))].Value);
                        }
                        sb.Append(Environment.NewLine);
                    }
                    Clipboard.SetText(sb.ToString());
                    textBox1.Text = sb.ToString();
                }
            }
        }
    }
}
