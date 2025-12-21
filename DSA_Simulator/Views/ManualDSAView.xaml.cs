using DSA_DigitalSignature.Models;
using DSA_DigitalSignature.Services;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;



namespace DSA_DigitalSignature.Views
{

    public static class BigIntegerExtensions
    {
        public static int GetBitLength(this BigInteger value)
        {
            if (value.Sign == 0) return 0;
            return (int)Math.Ceiling(BigInteger.Log(value, 2));
        }
    }
    public partial class ManualDSAView : Page
    {
        string? manualSigPath;
        ManualSignatureFile? loadedSig;


        private DsaParameters ReadParams()
        {
            if (string.IsNullOrWhiteSpace(txtP.Text) ||
                string.IsNullOrWhiteSpace(txtQ.Text) ||
                string.IsNullOrWhiteSpace(txtG.Text) ||
                string.IsNullOrWhiteSpace(txtX.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin");
                throw new InvalidOperationException();
            }


            BigInteger p = BigInteger.Parse(txtP.Text);
            BigInteger q = BigInteger.Parse(txtQ.Text);
            BigInteger g = BigInteger.Parse(txtG.Text);
            BigInteger x = BigInteger.Parse(txtX.Text);

            return new DsaParameters
            {
                P = p,
                Q = q,
                G = g,
                X = x,
                Y = BigInteger.ModPow(g, x, p)
            };
        }


        DsaSignature? sig;
        public ManualDSAView()
        {
            InitializeComponent();
        }

        private void LoadSample(object s, System.Windows.RoutedEventArgs e)
        {
            txtP.Text = "23";
            txtQ.Text = "11";
            txtG.Text = "4";
            txtX.Text = "3";
            txtK.Text = "7";
        }

        private void Sign(object sender, RoutedEventArgs e)
        {
            // ===== [THÊM] KIỂM TRA RỖNG =====
            if (string.IsNullOrWhiteSpace(txtMessage.Text) ||
                string.IsNullOrWhiteSpace(txtK.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin");
                return;
            }

            DsaParameters p;
            try
            {
                p = ReadParams();
            }
            catch
            {
                return;
            }

            // =====  KIỂM TRA HỢP LỆ DSA =====
            BigInteger k = BigInteger.Parse(txtK.Text);

            if (p.P <= p.Q)
            {
                MessageBox.Show("Tham số không hợp lệ: p phải lớn hơn q");
                return;
            }

            if ((p.P - 1) % p.Q != 0)
            {
                MessageBox.Show("Tham số không hợp lệ: q phải chia hết cho (p - 1)");
                return;
            }

            if (p.G <= 1 || p.G >= p.P)
            {
                MessageBox.Show("Tham số không hợp lệ: 1 < g < p");
                return;
            }

            if (BigInteger.ModPow(p.G, p.Q, p.P) != 1)
            {
                MessageBox.Show("Tham số không hợp lệ: g^q mod p ≠ 1");
                return;
            }

            if (p.X <= 0 || p.X >= p.Q)
            {
                MessageBox.Show("Tham số không hợp lệ: 1 ≤ x < q");
                return;
            }

            if (k <= 1 || k >= p.Q)
            {
                MessageBox.Show("Tham số không hợp lệ: 1 < k < q");
                return;
            }

            if (BigInteger.GreatestCommonDivisor(k, p.Q) != 1)
            {
                MessageBox.Show("Tham số không hợp lệ: k không có nghịch đảo modulo q");
                return;
            }


            string message = txtMessage.Text;
            BigInteger H = HashService.ComputeSHA256(message);

            BigInteger y = BigInteger.ModPow(p.G, p.X, p.P);
            BigInteger r = BigInteger.ModPow(p.G, k, p.P) % p.Q;

            BigInteger kInv = DsaMathService.ModInverse(k, p.Q);
            BigInteger s = (kInv * (H + p.X * r)) % p.Q;

            sig = new DsaSignature { R = r, S = s };

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("KẾT QUẢ KÝ (SIGNING)");
            sb.AppendLine();
            sb.AppendLine("Tham số đầu vào:");
            sb.AppendLine($"p = {p.P}    q = {p.Q}    g = {p.G}    x = {p.X}    k = {k}");
            sb.AppendLine($"Message: \"{message}\"");
            sb.AppendLine();
            sb.AppendLine("============================");

            sb.AppendLine("CHỮ KÝ SỐ DSA");
            sb.AppendLine($"Signature = (r, s) = ({r}, {s})");

            txtSignSteps.Text = sb.ToString();
            txtResult.Text = "✔ Ký thành công";
        }
        private void GenerateKey(object sender, RoutedEventArgs e)
        {
            var dsa = DsaKeyGenerator.Generate2048BitKey();

            txtP.Text = dsa.P.ToString();
            txtQ.Text = dsa.Q.ToString();
            txtG.Text = dsa.G.ToString();
            txtX.Text = dsa.X.ToString();

            txtResult1.Text = "✔ Đã tạo khóa DSA 2048-bit (chuẩn thực tế)";
        }


        private void Verify(object sender, RoutedEventArgs e)
        {

            if (sig == null)
            {
                MessageBox.Show("Vui lòng ký trước khi xác thực");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMessage1.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin");
                return;
            }

            DsaParameters p;
            try
            {
                p = ReadParams();
            }
            catch
            {
                return;
            }

            string message = txtMessage1.Text;
            BigInteger H = HashService.ComputeSHA256(message);

            BigInteger r = sig.R;
            BigInteger s = sig.S;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("KẾT QUẢ XÁC THỰC (VERIFY)");
            sb.AppendLine();
            sb.AppendLine("Tham số sử dụng:");
            sb.AppendLine($"p = {p.P}    q = {p.Q}    g = {p.G}");
            sb.AppendLine($"Public key y = {p.Y}");
            sb.AppendLine($"Signature (r, s) = ({r}, {s})");
            sb.AppendLine($"Message: \"{message}\"");
            sb.AppendLine();
            sb.AppendLine("======================================");
            sb.AppendLine("w = s⁻¹ mod q");
            sb.AppendLine($"  = {s}⁻¹ mod {p.Q}");
            BigInteger w = DsaMathService.ModInverse(s, p.Q);

            sb.AppendLine($"  = {w}");
            sb.AppendLine();
            sb.AppendLine("======================================");
            BigInteger u1 = (H * w) % p.Q;
            sb.AppendLine($"u1= {u1}");


            BigInteger u2 = (r * w) % p.Q;
            sb.AppendLine($"u2= {u2}");

            // ===== BƯỚC 3: TÍNH v =====
            sb.AppendLine("TÍNH v");
            sb.AppendLine("v = (g^u1 × y^u2 mod p) mod q");
            sb.AppendLine($"  = ({p.G}^{u1} × {p.Y}^{u2} mod {p.P}) mod {p.Q}");

            BigInteger gu1 = BigInteger.ModPow(p.G, u1, p.P);
            BigInteger yu2 = BigInteger.ModPow(p.Y, u2, p.P);

            sb.AppendLine($"  = ({gu1} × {yu2} mod {p.P}) mod {p.Q}");

            BigInteger v = (gu1 * yu2 % p.P) % p.Q;

            sb.AppendLine($"  = {v}");
            sb.AppendLine();
            sb.AppendLine("======================================");

            // ===== KẾT LUẬN =====
            sb.AppendLine("KẾT LUẬN");

            if (v == r)
            {
                sb.AppendLine("✔ KẾT LUẬN: CHỮ KÝ HỢP LỆ");
                MessageBox.Show("✔ CHỮ KÝ HỢP LỆ",
                    "Xác thực thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                sb.AppendLine("✖ KẾT LUẬN: CHỮ KÝ KHÔNG HỢP LỆ");
                MessageBox.Show("✖ CHỮ KÝ KHÔNG HỢP LỆ",
                    "Cảnh báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            txtVerifySteps.Text = sb.ToString();
        }
        private void TaoFileChuKi(object sender, RoutedEventArgs e)
        {
            if (sig == null)
            {
                MessageBox.Show("⚠ Chưa có chữ ký để lưu");
                return;
            }
            DsaParameters p;
            try
            {
                p = ReadParams();
            }
            catch
            {
                MessageBox.Show("Không thể đọc tham số DSA");
                return;
            }


            var sigFile = new ManualSignatureFile
            {
                P = p.P.ToString(),
                Q = p.Q.ToString(),
                G = p.G.ToString(),
                Y = p.Y.ToString(),
                R = sig.R.ToString(),
                S = sig.S.ToString()
            };

            var save = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "DSA Signature (*.sig.json)|*.sig.json",
                FileName = "manual_dsa_signature.sig.json"
            };

            if (save.ShowDialog() == true)
            {
                JsonService.Save(save.FileName, sigFile);
                MessageBox.Show("✔ Đã tạo file chữ ký thành công");
            }
        }



        private void ChonFileChuKi(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage1.Text))
            {
                MessageBox.Show("⚠ Vui lòng nhập văn bản cần xác thực vào ô bên phải");
                return;
            }

            var open = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "DSA Signature (*.sig.json)|*.sig.json"
            };

            if (open.ShowDialog() != true)
                return;

            // ===== CHỈ LOAD FILE DO TAOFILECHUKI TẠO =====
            loadedSig = JsonService.Load<ManualSignatureFile>(open.FileName);

            if (loadedSig == null ||
                string.IsNullOrWhiteSpace(loadedSig.P) ||
                string.IsNullOrWhiteSpace(loadedSig.R))
            {
                MessageBox.Show("❌ File chữ ký không hợp lệ hoặc sai định dạng");
                return;
            }

            // ===== PARSE DỮ LIỆU =====
            BigInteger p = BigInteger.Parse(loadedSig.P);
            BigInteger q = BigInteger.Parse(loadedSig.Q);
            BigInteger g = BigInteger.Parse(loadedSig.G);
            BigInteger y = BigInteger.Parse(loadedSig.Y);
            BigInteger r = BigInteger.Parse(loadedSig.R);
            BigInteger s = BigInteger.Parse(loadedSig.S);

            string message = txtMessage1.Text;
            BigInteger H = HashService.ComputeSHA256(message);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📘 XÁC THỰC CHỮ KÝ TỪ FILE");
            sb.AppendLine("--------------------------------");
            sb.AppendLine("📥 Tham số nạp từ file chữ ký:");
            sb.AppendLine($"p = {p}");
            sb.AppendLine($"q = {q}");
            sb.AppendLine($"g = {g}");
            sb.AppendLine($"y = {y}");
            sb.AppendLine($"r = {r}");
            sb.AppendLine($"s = {s}");
            sb.AppendLine();

            // ===== BƯỚC 1 =====
            BigInteger w = DsaMathService.ModInverse(s, q);
            sb.AppendLine($"w = s⁻¹ mod q = {w}");

            // ===== BƯỚC 2 =====
            BigInteger u1 = (H * w) % q;
            BigInteger u2 = (r * w) % q;
            sb.AppendLine($"u1 = H×w mod q = {u1}");
            sb.AppendLine($"u2 = r×w mod q = {u2}");

            // ===== BƯỚC 3 =====
            BigInteger v =
                (BigInteger.ModPow(g, u1, p)
                * BigInteger.ModPow(y, u2, p)
                % p) % q;

            sb.AppendLine($"v = (g^u1 × y^u2 mod p) mod q = {v}");
            sb.AppendLine();

            // ===== KẾT LUẬN =====
            if (v == r)
            {
                sb.AppendLine("✔ KẾT LUẬN: CHỮ KÝ HỢP LỆ");
                MessageBox.Show("✔ CHỮ KÝ HỢP LỆ",
                    "Xác thực thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                sb.AppendLine("✖ KẾT LUẬN: CHỮ KÝ KHÔNG HỢP LỆ");
                MessageBox.Show("✖ CHỮ KÝ KHÔNG HỢP LỆ",
                    "Cảnh báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            txtVerifySteps.Text = sb.ToString();
        }

        private void TaoFileMessage(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                MessageBox.Show("⚠ Vui lòng nhập văn bản cần lưu");
                return;
            }

            var save = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Word Document (*.docx)|*.docx",
                FileName = "message.docx"
            };

            if (save.ShowDialog() != true)
                return;

            using (WordprocessingDocument doc =
                WordprocessingDocument.Create(save.FileName, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                Paragraph para = new Paragraph();
                Run run = new Run();
                run.AppendChild(new Text(txtMessage.Text));

                para.Append(run);
                body.Append(para);
            }

            MessageBox.Show("✔ Đã lưu văn bản thành file DOCX");
        }


    }

}




