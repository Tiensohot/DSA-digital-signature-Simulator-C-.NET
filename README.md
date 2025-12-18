🔐 DSA Digital Signature Simulator (C# .NET)

## 📌 Giới thiệu
**DSA Digital Signature Simulator** là phần mềm mô phỏng **thuật toán chữ ký số DSA (Digital Signature Algorithm)**, được phát triển bằng **C# WPF trên nền tảng .NET 8**.

Phần mềm giúp người học hiểu rõ:
- Cách tạo chữ ký số DSA
- Quy trình xác thực chữ ký
- Các bước tính toán chi tiết trong thuật toán (thay số → kết quả)
---

## ✨ Tính năng chính
- ✍️ Ký chữ ký số DSA với **văn bản nhập tay**
- 📁 Ký chữ ký số DSA với **tệp tin**
- 🔍 Xác thực chữ ký số
- 🧮 Hiển thị **chi tiết từng bước tính toán DSA**
- 💾 Xuất và đọc **file chữ ký (.sig.json)**

---

## 🛠️ Công nghệ sử dụng
- **Ngôn ngữ:** C#
- **Framework:** .NET 8
- **Giao diện:** WPF
- **Thuật toán băm:** SHA-256
- **Thuật toán chữ ký số:** DSA

---

## 📂 Cấu trúc project
```text
DSA_DigitalSignature/
│
├── Views/          # Giao diện người dùng (ManualDSAView, FileDSAView)
├── Models/         # Các lớp dữ liệu (DsaParameters, SignatureFile, ...)
├── Services/       # Xử lý băm, ký và xác thực chữ ký
├── DSA_DigitalSignature.sln
└── README.md


##▶️ Hướng dẫn chạy chương trình
1️⃣ Clone repository
git clone https://github.com/Tiensohot/DSA-digital-signature-Simulator-C-.NET.git

2️⃣ Mở project

Mở file DSA_DigitalSignature.sln bằng Visual Studio 2022

3️⃣ Chạy chương trình

Nhấn F5 hoặc Run
