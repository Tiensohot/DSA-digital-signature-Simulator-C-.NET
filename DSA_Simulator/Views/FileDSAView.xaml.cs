using DSA_DigitalSignature.Models;
using DSA_DigitalSignature.Services;
using Microsoft.Win32;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace DSA_DigitalSignature.Views
{

    public partial class FileDSAView : Page
    {

        private DsaParams ReadParams()
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

            BigInteger y = BigInteger.ModPow(g, x, p);

            return new DsaParams
            {
                P = p,
                Q = q,
                G = g,
                X = x,
                Y = y
            };
        }



        string? filePath;
        public FileDSAView()
        {
            InitializeComponent();
        }

        private void ChooseFile(object s, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            if (d.ShowDialog() == true)
                filePath = d.FileName;
                txtFilePath.Text = filePath;
        }

        private void SignFile(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Chưa chọn file cần ký");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtK.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin");
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
            string content;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                content = sr.ReadToEnd();
            }

            BigInteger H = HashService.ComputeSHA256(content);

            BigInteger k = BigInteger.Parse(txtK.Text);
            BigInteger y = BigInteger.ModPow(p.G, p.X, p.P);

            BigInteger r = BigInteger.ModPow(p.G, k, p.P) % p.Q;
            BigInteger kInv = DsaMathService.ModInverse(k, p.Q);
            BigInteger s = (kInv * (H + p.X * r)) % p.Q;

            var sigFile = new SignatureFile
            {
                Algorithm = "DSA",
                Hash = H.ToString(),
                R = r.ToString(),
                S = s.ToString(),
                PublicKeyY = y.ToString(),
                P = p.P.ToString(),
                Q = p.Q.ToString(),
                G = p.G.ToString()
            };

            var save = new SaveFileDialog
            {
                Filter = "DSA Signature (*.sig.json)|*.sig.json",
                FileName = Path.GetFileName(filePath) + ".sig.json"
            };

            if (save.ShowDialog() == true)
            {
                JsonService.Save(save.FileName, sigFile);
                MessageBox.Show("✔ Ký file & xuất chữ ký thành công");
            }
        }

        string? signaturePath;
     

        string? sigPath;

        private void ChooseSignatureFile(object sender, RoutedEventArgs e)
        {
            var open = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "DSA Signature (*.sig.json)|*.sig.json|All files (*.*)|*.*"
            };

            if (open.ShowDialog() == true)
            {
                signaturePath = open.FileName;
                txtSignaturePath.Text = signaturePath;
            }
        }

        private void VerifyFile(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath) ||
                string.IsNullOrEmpty(signaturePath))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin");
                return;
            }

            var sigFile = JsonService.Load<SignatureFile>(signaturePath);

            string content = File.ReadAllText(filePath);
            BigInteger H = HashService.ComputeSHA256(content);

            BigInteger p = BigInteger.Parse(sigFile.P);
            BigInteger q = BigInteger.Parse(sigFile.Q);
            BigInteger g = BigInteger.Parse(sigFile.G);
            BigInteger y = BigInteger.Parse(sigFile.PublicKeyY);
            BigInteger r = BigInteger.Parse(sigFile.R);
            BigInteger s = BigInteger.Parse(sigFile.S);

            BigInteger w = DsaMathService.ModInverse(s, q);
            BigInteger u1 = (H * w) % q;
            BigInteger u2 = (r * w) % q;

            BigInteger v =
                (BigInteger.ModPow(g, u1, p)
                * BigInteger.ModPow(y, u2, p)
                % p) % q;

            bool ok = v == r;

            txtStatus.Text = ok ? "✔ File HỢP LỆ" : "✖ File KHÔNG HỢP LỆ";
            txtResult.Text = $"v = {v}\nr = {r}";
        }
    }
}
