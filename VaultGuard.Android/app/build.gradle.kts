plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.kotlin.serialization)
    alias(libs.plugins.ksp)
    alias(libs.plugins.hilt)
}

android {
    namespace = "com.vaultguard.app"
    compileSdk = 35

    defaultConfig {
        // NOT renamed to the dotnetappdevni package prefix (unlike the MAUI app's applicationId) -
        // com.vaultguard.app is load-bearing here: it's referenced by the passkey/App Links Digital
        // Asset Links config (VaultGuard.Web's /.well-known/assetlinks.json, driven by
        // AndroidPackageName in appsettings.json) and iOS's associated-domains entitlements.
        // Renaming it would break passkey verification across platforms unless all of those are
        // updated in lockstep - flagging this rather than doing it silently.
        applicationId = "com.vaultguard.app"
        minSdk = 33
        targetSdk = 35
        versionCode = 1
        // Overridable via -PversionName=X.Y.Z (build-android-native.yml passes the same shared
        // release-vX.Y.Z version every other app publishes under) - falls back to a fixed default
        // for local/Android Studio builds where that property isn't set.
        versionName = (project.findProperty("versionName") as String?) ?: "1.0.0"
        vectorDrawables { useSupportLibrary = true }
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }

    buildFeatures { compose = true }

    packaging {
        resources { excludes += "/META-INF/{AL2.0,LGPL2.1}" }
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.runtime.compose)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(libs.androidx.activity.compose)
    implementation(libs.kotlinx.coroutines.android)

    // Compose
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.graphics)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.compose.material3)
    implementation(libs.androidx.compose.material.icons.extended)
    implementation(libs.androidx.navigation.compose)
    debugImplementation(libs.androidx.compose.ui.tooling)

    // Hilt
    implementation(libs.hilt.android)
    ksp(libs.hilt.compiler)
    implementation(libs.androidx.hilt.navigation.compose)

    // Networking (API mode)
    implementation(libs.retrofit)
    implementation(libs.retrofit.kotlinx.serialization)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging)
    implementation(libs.kotlinx.serialization.json)

    // Local SQLite vault (Room)
    implementation(libs.room.runtime)
    implementation(libs.room.ktx)
    ksp(libs.room.compiler)

    // Config + secrets
    implementation(libs.androidx.datastore.preferences)
    implementation(libs.androidx.security.crypto)

    // QR scanning (CameraX + ML Kit)
    implementation(libs.camerax.core)
    implementation(libs.camerax.camera2)
    implementation(libs.camerax.lifecycle)
    implementation(libs.camerax.view)
    implementation(libs.mlkit.barcode)
    implementation(libs.accompanist.permissions)
    // QR code generation (device setup)
    implementation(libs.zxing.core)

    // Biometric second factor
    implementation(libs.androidx.biometric)

    // Passkeys (WebAuthn via Credential Manager)
    implementation(libs.androidx.credentials)
    implementation(libs.androidx.credentials.play.services)
}
