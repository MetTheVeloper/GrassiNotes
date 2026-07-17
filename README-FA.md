# GrassiNotes 2.1.4

این نسخه یک Hotfix برای پایداری جهت و تراز پاراگراف و List هنگام تایپ و Enter است.

## اصلاحات

- `Ctrl + Right Shift` اکنون RTL و راست‌چین را به‌صورت یکپارچه روی پاراگراف و List اعمال می‌کند.
- `Ctrl + Left Shift` اکنون LTR و چپ‌چین را به‌صورت یکپارچه اعمال می‌کند.
- تراز RTL/LTR پس از تایپ، Enter، Bullet و Numbering حفظ می‌شود.
- پس از هر تغییر متن، RTL به‌صورت اجباری Right و LTR به‌صورت اجباری Left تراز می‌شود.
- جهت محل تایپ (Caret/Selection) با Paragraph و List همگام می‌شود تا WPF نتواند تراز را پس از اولین کاراکتر برگرداند.
- جهت و تراز پاراگراف جدید پس از Enter از پاراگراف قبلی به ارث می‌رسد.
- تست رگرسیون جهت و تراز در Workflow ویندوز اجرا می‌شود.
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
