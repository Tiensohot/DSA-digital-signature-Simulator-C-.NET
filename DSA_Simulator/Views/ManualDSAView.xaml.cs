using DSA_DigitalSignature.Models;
using DSA_DigitalSignature.Services;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DSA_DigitalSignature.Views
{
    public partial class ManualDSAView : Page
    {
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

            string message = txtMessage.Text;
            BigInteger k = BigInteger.Parse(txtK.Text);

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
            sb.AppendLine("======================================");
            sb.AppendLine("BƯỚC 1: TÍNH PUBLIC KEY (y)");
            sb.AppendLine($"y = g^x mod p = {p.G}^{p.X} mod {p.P} = {y}");
            sb.AppendLine($"→ y = {y}");
            sb.AppendLine();
            sb.AppendLine("======================================");
            sb.AppendLine("BƯỚC 2: HASH MESSAGE");
            sb.AppendLine($"H(m) = SHA256(\"{message}\")");
            sb.AppendLine($"→ H(m) = {H}");
            sb.AppendLine();
            sb.AppendLine("======================================");
            sb.AppendLine("BƯỚC 3: TÍNH r");
            sb.AppendLine($"r = (g^k mod p) mod q");
            sb.AppendLine($"  = ({p.G}^{k} mod {p.P}) mod {p.Q}");
            sb.AppendLine($"  = {BigInteger.ModPow(p.G, k, p.P)} mod {p.Q}");
            sb.AppendLine($"→ r = {r}");
            sb.AppendLine();
            sb.AppendLine("======================================");
            sb.AppendLine("BƯỚC 4: TÍNH s");
            sb.AppendLine($"k⁻¹ mod q = {kInv}");
            sb.AppendLine($"x × r = {p.X} × {r} = {p.X * r}");
            sb.AppendLine($"H(m) + x×r = {H} + {p.X * r}");
            sb.AppendLine($"s = k⁻¹ × (H(m) + x×r) mod q");
            sb.AppendLine($"  = {kInv} × ({H + p.X * r}) mod {p.Q}");
            sb.AppendLine($"→ s = {s}");
            sb.AppendLine();
            sb.AppendLine("======================================");
            sb.AppendLine("CHỮ KÝ SỐ DSA");
            sb.AppendLine($"Signature = (r, s) = ({r}, {s})");

            txtSignSteps.Text = sb.ToString();
            txtResult.Text = "✔ Ký thành công";
        }



        private void Verify(object sender, RoutedEventArgs e)
        {
            if (sig == null)
            {
                MessageBox.Show("Vui lòng ký trước khi xác thực");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMessage.Text))
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

            string message = txtMessage.Text;
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

            // ===== BƯỚC 1: TÍNH w =====
            sb.AppendLine("BƯỚC 1: TÍNH w");
            sb.AppendLine("w = s⁻¹ mod q");
            sb.AppendLine($"  = {s}⁻¹ mod {p.Q}");

            BigInteger w = DsaMathService.ModInverse(s, p.Q);

            sb.AppendLine($"  = {w}");
            sb.AppendLine();
            sb.AppendLine("======================================");

            // ===== BƯỚC 2: TÍNH u1, u2 =====
            sb.AppendLine("BƯỚC 2: TÍNH u1, u2");

            sb.AppendLine("u1 = H(m) × w mod q");
            sb.AppendLine($"   = {H} × {w} mod {p.Q}");

            BigInteger u1 = (H * w) % p.Q;

            sb.AppendLine($"   = {u1}");
            sb.AppendLine();

            sb.AppendLine("u2 = r × w mod q");
            sb.AppendLine($"   = {r} × {w} mod {p.Q}");

            BigInteger u2 = (r * w) % p.Q;

            sb.AppendLine($"   = {u2}");
            sb.AppendLine();
            sb.AppendLine("======================================");

            // ===== BƯỚC 3: TÍNH v =====
            sb.AppendLine("BƯỚC 3: TÍNH v");
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
                sb.AppendLine("v = r → CHỮ KÝ HỢP LỆ");
                txtResult.Text = "✔ CHỮ KÝ HỢP LỆ";
            }
            else
            {
                sb.AppendLine("v ≠ r → CHỮ KÝ KHÔNG HỢP LỆ");
                txtResult.Text = "✖ CHỮ KÝ KHÔNG HỢP LỆ";
            }

            txtVerifySteps.Text = sb.ToString();
        }
    }
}



