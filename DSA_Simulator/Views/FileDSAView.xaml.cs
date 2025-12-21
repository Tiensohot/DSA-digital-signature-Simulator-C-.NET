using DocumentFormat.OpenXml.Packaging;
using DSA_DigitalSignature.Models;
using DSA_DigitalSignature.Services;
using Microsoft.Win32;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml.Wordprocessing;
using DSA_DigitalSignature.Extensions;



namespace DSA_DigitalSignature.Views
{

    public partial class FileDSAView : Page
    {
        string? filePath;          // file cần ký / xác thực
        string? signaturePath;     // file chữ ký
        SignatureFile? loadedSig;  // dữ liệu chữ ký

        public FileDSAView()
        {
            InitializeComponent();
        }
        private void GenerateKey2048(object sender, RoutedEventArgs e)
        {
            var key = DsaKeyGenerator.Generate2048BitKey();

            txtP.Text = key.P.ToString();
            txtQ.Text = key.Q.ToString();
            txtG.Text = key.G.ToString();
            txtX.Text = key.X.ToString();

            MessageBox.Show("✔ Đã tạo tham số DSA 2048-bit ngẫu nhiên");
        }
        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("❌ Chưa chọn file cần ký", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtP.Text) ||
                string.IsNullOrWhiteSpace(txtQ.Text) ||
                string.IsNullOrWhiteSpace(txtG.Text) ||
                string.IsNullOrWhiteSpace(txtX.Text) ||
                string.IsNullOrWhiteSpace(txtK.Text))
            {
                MessageBox.Show("❌ Vui lòng nhập đầy đủ p, q, g, x, k", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
        private void ValidateDsaParams(DsaParams p, BigInteger k)
        {
            if (!p.P.IsProbablePrime())
                throw new ArgumentException("p không phải là số nguyên tố");

            if (!p.Q.IsProbablePrime())
                throw new ArgumentException("q không phải là số nguyên tố");

            if ((p.P - 1) % p.Q != 0)
                throw new ArgumentException("q phải chia hết (p - 1)");

            if (p.G <= 1 || p.G >= p.P)
                throw new ArgumentException("g phải thỏa 1 < g < p");

            if (BigInteger.ModPow(p.G, p.Q, p.P) != 1)
                throw new ArgumentException("g^q mod p ≠ 1 → g không hợp lệ");

            if (p.X <= 0 || p.X >= p.Q)
                throw new ArgumentException("x phải thỏa 1 ≤ x < q");

            if (k <= 1 || k >= p.Q)
                throw new ArgumentException("k phải thỏa 1 < k < q");

            if (BigInteger.GreatestCommonDivisor(k, p.Q) != 1)
                throw new ArgumentException("k không có nghịch đảo modulo q");
        }

        public static string ReadDocxText(string filePath)
        {
            var sb = new StringBuilder();

            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;

                foreach (var para in body.Elements<Paragraph>())
                {
                    sb.AppendLine(para.InnerText);
                }
            }

            return sb.ToString();
        }
        private void ChooseFile(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog
            {
                Filter = "Text / Word (*.txt;*.docx)|*.txt;*.docx|All files (*.*)|*.*"
            };

            if (open.ShowDialog() == true)
            {
                filePath = open.FileName;
                txtFilePath.Text = filePath;

                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".docx")
                {
                    txtMassage.Text = ReadDocxText(filePath);
                }
                else
                {
                    txtMassage.Text = File.ReadAllText(
                        filePath,
                        Encoding.UTF8
                    );
                }
            }
        }


        // ===================== ĐỌC THAM SỐ =====================
        private DsaParams ReadParams()
        {
            if (string.IsNullOrWhiteSpace(txtP.Text) ||
                string.IsNullOrWhiteSpace(txtQ.Text) ||
                string.IsNullOrWhiteSpace(txtG.Text) ||
                string.IsNullOrWhiteSpace(txtX.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tham số DSA");
                throw new InvalidOperationException();
            }

            BigInteger p = BigInteger.Parse(txtP.Text);
            BigInteger q = BigInteger.Parse(txtQ.Text);
            BigInteger g = BigInteger.Parse(txtG.Text);
            BigInteger x = BigInteger.Parse(txtX.Text);

            return new DsaParams
            {
                P = p,
                Q = q,
                G = g,
                X = x,
                Y = BigInteger.ModPow(g, x, p)
            };
        }

        // ===================== KÝ FILE =====================
        private void SignFile(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("Chưa chọn file cần ký");
                    return;
                }

                if (!ValidateInput())
                    return;

                if (string.IsNullOrWhiteSpace(txtK.Text))
                {
                    MessageBox.Show("Vui lòng nhập k");
                    return;
                }

                DsaParams p;
                try
                {
                    p = ReadParams();
                }
                catch
                {
                    return;
                }

                string content = File.ReadAllText(filePath);
                BigInteger H = HashService.ComputeSHA256(content);

                BigInteger k = BigInteger.Parse(txtK.Text);
                BigInteger r = BigInteger.ModPow(p.G, k, p.P) % p.Q;
                BigInteger s = (DsaMathService.ModInverse(k, p.Q) * (H + p.X * r)) % p.Q;

                var sigFile = new SignatureFile
                {
                    P = p.P.ToString(),
                    Q = p.Q.ToString(),
                    G = p.G.ToString(),
                    Y = p.Y.ToString(),
                    R = r.ToString(),
                    S = s.ToString()
                };
                ValidateDsaParams(p, k);

                var save = new SaveFileDialog
                {
                    Filter = "DSA Signature (*.sig.json)|*.sig.json",
                    FileName = Path.GetFileName(filePath) + ".sig.json"
                };

                if (save.ShowDialog() == true)
                {
                    JsonService.Save(save.FileName, sigFile);
                    MessageBox.Show("✔ Đã ký file & xuất chữ ký");
                    txtStatus.Text = "✔ Ký thành công";
                }

                BigInteger y = BigInteger.ModPow(p.G, p.X, p.P);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("🔐 Cặp khóa:");
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"Private key x = {p.X}");
                sb.AppendLine($"Public key y = {y}");
                sb.AppendLine();
                sb.AppendLine("🔎 HASH FILE (SHA-256)");
                sb.AppendLine(H.ToString());

                txtMessage1.Text = sb.ToString();
            }
            catch (FormatException)
            {
                MessageBox.Show(
                    "❌ Tham số nhập vào không hợp lệ (phải là số nguyên).",
                    "Lỗi dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    "❌ Tham số DSA không hợp lệ:\n" + ex.Message,
                    "Lỗi tham số DSA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            catch (IOException ex)
            {
                MessageBox.Show(
                    "❌ Lỗi đọc/ghi file:\n" + ex.Message,
                    "Lỗi file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (ArithmeticException ex)
            {
                MessageBox.Show(
                    "❌ Lỗi toán học DSA (có thể k không có nghịch đảo modulo q):\n" + ex.Message,
                    "Lỗi DSA",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "❌ Lỗi không xác định:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        // ===================== CHỌN FILE CHỮ KÝ (TỰ VERIFY) =====================
        private void ChooseSignatureFile(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("⚠ Vui lòng chọn file cần xác thực trước");
                return;
            }

            var open = new OpenFileDialog
            {
                Filter = "DSA Signature (*.sig.json)|*.sig.json"
            };

            if (open.ShowDialog() != true)
                return;

            signaturePath = open.FileName;
            loadedSig = JsonService.Load<SignatureFile>(signaturePath);

            // đổ tham số từ file chữ ký
            txtP.Text = loadedSig.P;
            txtQ.Text = loadedSig.Q;
            txtG.Text = loadedSig.G;
            txtX.Text = "";
            txtK.Text = "";
            txtSignaturePath.Text = signaturePath;
        }

        // ===================== VERIFY TỰ ĐỘNG =====================
        private void VerifyFromFile()
        {
            if (loadedSig == null || string.IsNullOrEmpty(filePath))
                return;

            BigInteger p = BigInteger.Parse(loadedSig.P);
            BigInteger q = BigInteger.Parse(loadedSig.Q);
            BigInteger g = BigInteger.Parse(loadedSig.G);
            BigInteger y = BigInteger.Parse(loadedSig.Y);
            BigInteger r = BigInteger.Parse(loadedSig.R);
            BigInteger s = BigInteger.Parse(loadedSig.S);

            string content = File.ReadAllText(filePath);
            BigInteger H = HashService.ComputeSHA256(content);

            BigInteger w = DsaMathService.ModInverse(s, q);
            BigInteger u1 = (H * w) % q;
            BigInteger u2 = (r * w) % q;

            BigInteger v =
                (BigInteger.ModPow(g, u1, p) *
                 BigInteger.ModPow(y, u2, p) % p) % q;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📘 XÁC THỰC CHỮ KÝ TỪ FILE");
            sb.AppendLine("--------------------------------");
            sb.AppendLine("Tham số nạp từ file:");
            sb.AppendLine($"p = {p}");
            sb.AppendLine($"q = {q}");
            sb.AppendLine($"g = {g}");
            sb.AppendLine($"y = {y}");
            sb.AppendLine();
            sb.AppendLine($"v = {v}");
            sb.AppendLine($"r = {r}");
            sb.AppendLine();
            sb.AppendLine(v == r
                ? "✔ KẾT LUẬN: FILE HỢP LỆ"
                : "✖ KẾT LUẬN: FILE KHÔNG HỢP LỆ");

            txtVerifySteps.Text = sb.ToString();
            txtStatus.Text = v == r ? "✔ File HỢP LỆ" : "✖ File KHÔNG HỢP LỆ";
        }
        private void VerifyFile(object sender, RoutedEventArgs e)
        {
            VerifyFromFile();
        }

    }
}
