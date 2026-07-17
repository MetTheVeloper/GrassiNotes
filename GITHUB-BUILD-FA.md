# راهنمای ساخت خودکار GrassiNotes با GitHub Actions

این پوشه آماده است تا مستقیماً داخل یک Repository جدید قرار بگیرد. فایل زیر کار Build را انجام می‌دهد:

```text
.github/workflows/build-windows.yml
```

## خروجی‌های هر Build

پس از موفقیت Workflow، چهار فایل ساخته می‌شوند:

- `GrassiNotes-2.1.3-x64.exe`
- `GrassiNotes-Portable-2.1.3.zip`
- `GrassiNotes-Setup-2.1.3.exe`
- `GrassiNotes-2.1.3-SHA256.txt`

## Build دستی در GitHub

1. وارد Repository شوید.
2. تب **Actions** را باز کنید.
3. از ستون سمت چپ **Build GrassiNotes for Windows** را انتخاب کنید.
4. دکمه **Run workflow** را بزنید.
5. پس از سبزشدن Workflow، همان اجرا را باز کنید.
6. پایین صفحه، در بخش **Artifacts**، فایل `GrassiNotes-2.1.3-Windows` را دانلود کنید.

## Build خودکار

هر Pull Request به شاخه `main` و هر Push به `main`، Build را به‌صورت خودکار اجرا می‌کند.

## ساخت Release قابل دانلود

برای ساخت Release، یک Tag دقیقاً مطابق Version پروژه ایجاد کنید. برای نسخه فعلی:

```text
v2.1.3
```

با Push شدن این Tag، Workflow علاوه بر Artifact، یک GitHub Release می‌سازد و فایل‌های EXE، Setup، Portable ZIP و Checksum را به آن پیوست می‌کند.

## تغییر شماره نسخه در آینده

شماره نسخه اصلی در فایل زیر است:

```text
GrassiNotes.csproj
```

مقدار این خط را تغییر دهید:

```xml
<Version>2.1.3</Version>
```

همچنین سه مقدار Version در `Properties/AssemblyInfo.cs` را با نسخه جدید هماهنگ کنید. سپس Commit و Push کنید.

## نکات

- Build روی ماشین مجازی Windows خود GitHub انجام می‌شود؛ روی کامپیوتر شما نصب .NET، Visual Studio یا C# لازم نیست.
- فایل Installer با Inno Setup ساخته می‌شود.
- فایل‌ها امضای دیجیتال ندارند، بنابراین SmartScreen ممکن است در اولین اجرا هشدار بدهد.
