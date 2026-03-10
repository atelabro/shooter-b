# Release Plan

## Accounts
- Google Play Console: set up
- App Store Connect: set up (Apple Developer membership active)
- AdMob: set up (Android and iOS app IDs configured)

## Completed
- [x] Android Build Support modules installed
- [x] Android Bundle ID: com.alab.duckoff
- [x] Android Min SDK: 25, Target SDK: 35
- [x] Android keystore created and stored in iCloud Drive
- [x] iOS Bundle ID: com.alab.duckoff
- [x] iOS Version: 1.0, Build: 1
- [x] iOS Team ID configured, Automatic Signing enabled
- [x] iOS User Tracking Usage Description set (AdMob ATT)
- [x] Dead code removed (OnBirdsKilledChanged)
- [x] Debug logging wrapped with GameLog (stripped in release builds)
- [x] smallAnimatedChe duck removed

## Remaining Before Build
- [x] Rewarded ads integration (pre-stage bonus: +2 lives / +ammo)
- [x] AdMob ad unit IDs configured

## Android Release Steps
1. File > Build Settings > Android > switch to Release mode
2. Build AAB (Android App Bundle)
3. Upload to Google Play Console as Internal Test
4. Promote to Closed Testing (Alpha)
5. Promote to Open Testing (Beta)
6. Promote to Production

## iOS Release Steps
1. File > Build Settings > iOS > Build (generates Xcode project)
2. Open Xcode project
3. Product > Archive
4. Distribute App > App Store Connect
5. TestFlight internal testers first (up to 100, instant)
6. TestFlight external testers (up to 10,000, ~24-48h Apple review)
7. Submit for App Store Review > Production
