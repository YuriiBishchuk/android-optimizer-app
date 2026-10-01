# 🤖 Android Optimizer — Phone & Tablet App (.NET 10 MAUI)
[![GitHub](https://img.shields.io/badge/GitHub-YuriiBishchuk%2Fandroid--optimizer--app-blue)](https://github.com/YuriiBishchuk/android-optimizer-app)

Керуй та оптимізуй будь-який Android пристрій (**TV, телефони, планшети, автомагнітоли, ТВ-бокси, IoT**) **прямо з телефона** по Wi-Fi (Wireless Debugging, Android 11+) або USB.
100% Без root, без ПК. Підключив → аудит → безпечний debloat → прискорив анімації → очистив кеш → заблокував витік заряду у фоні → захистив приватність.

> Підтримка бази знань Universal Debloater Alliance (`uad_lists.json`) та перевірених community-списків з офлайн-кешуванням на 7 днів.

![Platform](https://img.shields.io/badge/Platform-Android_8%2B%20(MAUI)-green)
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![License](https://img.shields.io/badge/License-MIT-orange)

---

## 🏛️ Архітектура Проєкту

Проєкт побудований на сучасному стеку .NET 10 MAUI з модульною чистою архітектурою:

```
android-optimizer-app/
├── src/
│   ├── TvOptimizer.Core/          # Чистий C# (Domain Logic, AuditEngine, Guard, ConfigSync, TweaksEngine)
│   ├── TvOptimizer.Transport/     # Pure C# ADB Transport (TLS 1.3/RSA pairing, TCP socket, shell stream)
│   └── TvOptimizer.App/           # .NET 10 MAUI кросплатформний мобільний додаток (Android)
├── tests/
│   ├── TvOptimizer.Core.Tests/    # Unit-тести безпеки (Safety Guard, ConfigParser, Rules)
│   └── TvOptimizer.Transport.Tests/# Тести ADB протоколу та mock-сокета
├── scripts/
│   ├── emulator-ctl.sh            # Управління headless QEMU/KVM емуляторами (Phone / TV)
│   ├── test-emulator-low.sh       # Повний цикл e2e-тестування на low-resource AVD
│   └── release-debug-apk.sh       # Автоматизована збірка та підпис релізного APK
└── artifacts/                     # Згенеровані підписані версії APK
```

---

## 🛡️ Модель Безпеки (Safety Guard & Invariants)

1. **Суворий захист системних лаунчерів (NeverTouch & UniversalProtected):**
   - `com.google.android.apps.tv.launcherx`
   - `com.google.android.tvlauncher`
   - `com.google.android.leanbacklauncher`
   - `com.android.systemui`, `com.google.android.gms`, `com.android.vending`
   - Системний лаунчер **НІКОЛИ НЕ ВИМИКАЄТЬСЯ**, що унеможливлює стан «чорного екрану».

2. **5 Рівнів Класифікації (Tiers):**
   - 🟢 **TIER_1 (Universal Safe Bloat):** Рекламні трекери, аналітика, невикористовувані промо-сервіси (feedback, adservices, printspooler тощо). Дозволено пакетне вимкнення.
   - 🔵 **TIER_2 (Curated Device Bloat):** Вузли конкретних моделей (Xiaomi, Philips, TCL, Chromecast), узгоджені спільнотою.
   - 🟡 **TIER_3 (Heuristics):** Підозрілі пакети з ознаками телеметрії. Тільки ручне поштучне підтвердження.
   - ⚪ **TIER_4 (Unidentified):** Користувацькі додатки та нейтральні системні бібліотеки.
   - 🔴 **TIER_5 (Protected / NeverTouch):** Системні компоненти та лаунчери. Захищені від вимкнення апаратно на рівні Guard.

3. **Гарантія Відкату (Rollback):**
   - Усі дії фіксуються в історії сесії та можуть бути миттєво скасовані однією кнопкою через `pm enable <package>`.

---

## 📱 Покроковий Процес Підключення (Pairing Walkthrough)

### Для Android 11+ (Wireless Debugging):
1. Відкрийте на ТВ: `Налаштування` → `Параметри пристрою` → `Для розробників` → `Бездротове налагодження` (Увімкнути).
2. Натисніть `Підключити пристрій за допомогою коду підключення`.
3. На ТВ відобразиться:
   - **IP-адреса та порт створення пари** (наприклад, `192.168.0.45:37891`)
   - **6-значний код підключення** (наприклад, `123456`)
4. У додатку **Android TV Optimizer**:
   - Введіть IP, Pairing Port та Код підключення → натисніть **«Створити пару»**.
   - Після успішного створення пари введіть основний порт налагодження та натисніть **«Підключитися»**.

### Специфіка для Xiaomi TV A Pro 42 (2026):
- Порт бездротового налагодження змінюється після кожного перезавантаження ТВ — в додатку реалізовано швидке перепідключення з автозбереженням останньої IP-адреси.
- Лаунчер Xiaomi PatchWall (`com.mitv.tvhome.atv`) класифікується як TIER_2 (рекомендується замінювати на Projectivy Launcher).

---

## ⚡ Твіки Швидкодії (Performance Tweaks)

Додаток включає безпечний тюнінг через системні налаштування Android:
- **Пресет «Швидкий ТВ»:** анімації 0.5x, ліміт фонових процесів ≤ 4, увімкнення Doze для зниження енергоспоживання у простої.
- **Миттєве зчитування та застосування:** `window_animation_scale`, `transition_animation_scale`, `animator_duration_scale`.

---

## 🚀 Збірка та Реліз

```bash
# Збірка підписаного APK
bash scripts/release-debug-apk.sh

# Згенерований APK потрапляє в каталог artifacts/:
# artifacts/TvOptimizer-v0.1.0-<TIMESTAMP>-Signed.apk
```

---

## 🧪 Запуск Тестів

```bash
# Запуск Unit-тестів Core & Transport
dotnet test tests/TvOptimizer.Core.Tests/TvOptimizer.Core.Tests.csproj
dotnet test src/TvOptimizer.Transport.Tests/TvOptimizer.Transport.Tests.csproj

# Запуск e2e-тестів на емуляторі
bash scripts/test-emulator-low.sh
```

---

## 📄 Ліцензія та Подяки

- Ліцензія: [MIT](LICENSE).
- Джерело конфігурацій та правил оптимізації: [YuriiBishchuk/android-tv-optimizer](https://github.com/YuriiBishchuk/android-tv-optimizer).
- Подяка спільноті за списки перевірених bloatware-пакетів для Android TV.
