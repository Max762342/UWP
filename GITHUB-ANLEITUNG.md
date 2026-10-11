# 🚀 GitHub Actions – So geht's (Schritt für Schritt)

## Übersicht
1. **GitHub-Account erstellen** (kostenlos)
2. **Repository erstellen**
3. **Code hochladen**
4. **Automatischer Build** startet
5. **APPX herunterladen**
6. **Xbox installieren**

---

## Schritt 1: GitHub Account erstellen

1. Gehe zu **https://github.com**
2. Klicke auf **"Sign up"**
3. E-Mail eingeben, Passwort wählen, Username wählen
4. Bestätigen

---

## Schritt 2: Neues Repository erstellen

1. Einloggen auf GitHub
2. Oben rechts **"+"** klicken → **"New repository"**
3. **Repository name** eingeben: `XB-Launcher`
4. **Public** auswählen
5. ✅ **"Add a README file"** abwählen (das machen wir selbst)
6. Klicke **"Create repository"**

---

## Schritt 3: Code hochladen

GitHub zeigt dir eine Seite mit Befehlen. Kopiere den **"push an existing repository"**-Teil und führe ihn aus:

```powershell
cd C:\Users\Schüler\Desktop\XB-Launcher
git remote add origin https://github.com/DEIN_USERNAME/XB-Launcher.git
git branch -M main
git push -u origin main
```

**WICHTIG:** Ersetze `DEIN_USERNAME` mit deinem echten GitHub-Namen!

---

## Schritt 4: Build startet automatisch

1. Nach dem Push: Gehe zu deinem Repository
2. Klicke auf den Tab **"Actions"**
3. Du siehst: **"Build XB-Launcher for Xbox"** läuft
4. Warte **5-10 Minuten** bis alles grün ist ✅

---

## Schritt 5: APPX herunterladen

1. Klicke auf den **fertigen Build** (grüner Haken)
2. Scrolle runter zu **"Artifacts"**
3. Klicke auf **"XB-Launcher-Xbox"** zum Herunterladen
4. **ZIP entpacken** → Du bekommst:
   - `XB-Launcher.appx` (App)
   - `XB-Launcher.pfx` (Zertifikat)

---

## Schritt 6: Xbox installieren

1. **Xbox Dev Mode** aktiviert?
2. **Device Portal** öffnen: `https://<XBOX-IP>`
3. **Apps → Add → Installieren**
4. `XB-Launcher.appx` hochladen
5. **Fertig!** 🎮

---

## ⚡ Zusammenfassung

| Was | Wie |
|---|---|
| **Repository** | github.com → New repository |
| **Code hochladen** | `git push` (siehe Schritt 3) |
| **Build** | Automatisch bei jedem Push |
| **Download** | Actions → Build → Artifacts |
| **Xbox** | Device Portal → Apps installieren |

---

## 🔄 Neue Versionen

Jedes Mal wenn du den Code änderst und `git push` ausführst, wird **automatisch eine neue APPX gebaut**!

```powershell
git add .
git commit -m "Update"
git push
```

Dann einfach die neue APPX von GitHub Actions herunterladen.