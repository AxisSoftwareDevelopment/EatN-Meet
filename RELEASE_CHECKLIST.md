# 🚀 Release Checklist for Spots App

**⚠️ COMPLETE ALL STEPS BEFORE PUBLISHING TO GOOGLE PLAY STORE**

## 📋 Pre-Release Steps

### 1. ✅ Release Keystore Setup
- [ ] **Create release keystore** (if not already created):
  ```powershell
  keytool -genkey -v -keystore "C:\Repos\Spots\release.keystore" -alias spots_release -keyalg RSA -keysize 2048 -validity 10000
  ```
- [ ] **Backup keystore to 3 secure locations**:
  - [ ] Encrypted cloud storage (OneDrive/Google Drive/Dropbox)
  - [ ] External hard drive
  - [ ] Password manager (as file attachment)
- [ ] **Document keystore password** in password manager
- [ ] **Get release SHA-1**:
  ```powershell
  keytool -list -v -keystore "C:\Repos\Spots\release.keystore" -alias spots_release
  ```

### 2. ✅ Firebase & Google Cloud Configuration
- [ ] **Add release SHA-1 to Firebase**:
  - Go to: https://console.firebase.google.com/
  - Select `axissd-eatnmeet` project
  - Android app settings → Add SHA-1 fingerprint
- [ ] **Add release SHA-1 to Google Maps API**:
  - Go to: https://console.cloud.google.com/
  - APIs & Services → Credentials → Android Maps API key
  - Add package + SHA-1 restriction
- [ ] **Download updated `google-services.json`** from Firebase (if changed)

### 3. ✅ Code & Build Configuration
- [ ] Update version number in `Spots.csproj`:
  - Increment `<ApplicationVersion>`
  - Increment `<ApplicationDisplayVersion>`
- [ ] Set build configuration to **Release**
- [ ] Configure signing in Visual Studio:
  - Right-click Android project → Properties → Android Package Signing
  - Check "Sign the .APK file using the following keystore details"
  - Browse to `release.keystore`
  - Enter alias: `spots_release`
  - Enter passwords

### 4. ✅ Testing
- [ ] Test release build on physical device
- [ ] Verify Google Maps works (release SHA-1)
- [ ] Verify Firebase authentication works
- [ ] Test all critical features
- [ ] Check ProGuard/R8 hasn't broken anything

### 5. ✅ Final Checks
- [ ] No debug code or logs in release build
- [ ] All API keys are correct
- [ ] Privacy policy updated (if needed)
- [ ] Screenshots updated in Play Store listing
- [ ] Release notes prepared

### 6. ✅ Build & Upload
- [ ] Build **signed release APK/AAB**
- [ ] Upload to Google Play Console
- [ ] Submit for review

---

## 🔐 Security Reminders

**NEVER:**
- ❌ Commit keystore to Git (already in `.gitignore`)
- ❌ Share keystore via email/chat
- ❌ Use debug keystore for production

**ALWAYS:**
- ✅ Keep 3+ backups of release keystore
- ✅ Use strong, unique passwords
- ✅ Store passwords in password manager

---

## 📞 Emergency Contacts

**If keystore is lost:**
- ⚠️ You **CANNOT** update your app on Google Play
- ⚠️ You must publish as a **NEW app** with a new package name
- ⚠️ All existing users will need to **uninstall and reinstall**

**Backup locations:**
1. `[YOUR BACKUP LOCATION 1]`
2. `[YOUR BACKUP LOCATION 2]`
3. `[YOUR BACKUP LOCATION 3]`

---

**Last Updated:** `[DATE]`  
**Release Keystore Created:** `[DATE or "Not Yet Created"]`  
**Current App Version:** `[VERSION]`
