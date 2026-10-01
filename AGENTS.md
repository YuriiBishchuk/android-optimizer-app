# 🤖 Multi-Agent Orchestration & Development Guide

Цей проєкт розробляється в автономному тандемі **Antigravity IDE** (Головний Архітектор) та **Hermes Agent** (Kanban Orchestrator & PM).

---

## 📌 Загальні Правила для ШІ-Агентів

1. **Safety First:**
   - Жодних прямих `pm uninstall` без попередньої перевірки через `Guard.CanRemove()`.
   - Лаунчери Google TV (`launcherx`, `tvlauncher`, `leanbacklauncher`) та системні модулі входять у `UniversalProtected` і ніколи не видаляються.
2. **Ізоляція Середовища:**
   - Компіляція та тести виконуються виключно всередині контейнера `hermes-agent` під користувачем `hermes` (UID 1000).
   - Заборонено змінювати файли за межами `/projects/android-tv-optimizer-app`.
3. **Definition of Done (DoD):**
   - Код успішно компілюється під `.NET 10 MAUI (net10.0-android)`.
   - Всі Unit та Integration тести проходять (100% green).
   - Згенеровано та протестовано підписаний APK.
