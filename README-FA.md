# GrassiNotes 2.2.0

این نسخه لایه قالب‌بندی را با رفتار نسخه 1.3.0 به‌عنوان مرجع بازنویسی می‌کند. جهت و تراز پاراگراف اکنون یک تنظیم واحد هستند و عملیات Enter، Heading و List بدون اصلاحات زمان‌بندی‌شده در TextChanged انجام می‌شوند.

این نسخه یک Hotfix برای پایداری جهت و تراز پاراگراف و منوی رنگ است.

## اصلاحات

- `Ctrl + Right Shift` اکنون RTL و راست‌چین را به‌صورت یکپارچه روی پاراگراف و List اعمال می‌کند.
- `Ctrl + Left Shift` اکنون LTR و چپ‌چین را به‌صورت یکپارچه اعمال می‌کند.
- تراز RTL/LTR پس از تایپ، Enter، Bullet و Numbering حفظ می‌شود.
- جهت پاراگراف جدید پس از Enter از پاراگراف قبلی به ارث می‌رسد.
- Hover رنگ‌ها دیگر پس‌زمینه Swatch را با رنگ Hover جایگزین نمی‌کند.
- اصلاح Paste و فونت Vazirmatn نسخه 2.1.0 حفظ شده است.

# GrassiNotes 2.1.0

پروژه‌ی WPF برای Windows x64 و .NET 8.

## ساخت روی ویندوز

```powershell
dotnet publish .\GrassiNotes.csproj -c Release -r win-x64 --self-contained true
```

## تغییرات 2.1.0

- بازگردانی ابعاد، فاصله‌ها، رنگ‌ها و کنترل‌های رابط نسخه 1.3
- Title Bar و Window Controls اختصاصی
- منوی کلیک راست اختصاصی همراه Copy as Markdown
- منوی Heading اختصاصی با آیکون‌های N و H1 تا H4
- منوی Tray اختصاصی همراه میانبرهای G، T و Q
- اصلاح هم‌زمان FlowDirection و TextAlignment برای پاراگراف، Bullet و Numbering
- حفظ Direction در فایل‌های `.grass`
- یکسان‌سازی فونت Paste با Vazirmatn داخلی
- Scrollbar اختصاصی برای تم روشن و تاریک


## ساخت خودکار در GitHub

راهنمای کامل در فایل `GITHUB-BUILD-FA.md` قرار دارد.
