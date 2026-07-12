package com.vaultguard.app.ui.navigation

object Routes {
    const val SETUP = "setup"
    const val UNLOCK = "unlock"
    const val HOME = "home"
    const val SETTINGS = "settings"
    const val SCAN = "scan"
    const val QR_LOGIN = "qr_login"
    const val VAULTS = "vaults"
    const val CATEGORIES = "categories"
    const val SECURITY = "security"
    const val PASSKEYS = "passkeys"
    const val IMPORT = "import"
    const val IMPORT_1PASSWORD = "import_1password"
    const val PROFILE = "profile"
    const val ABOUT = "about"
    const val DEVICE_SETUP = "device_setup"

    const val DETAIL = "detail/{id}"
    fun detail(id: Int) = "detail/$id"

    // id = -1 means "new item"; scan=true auto-opens the QR scanner for a TOTP secret
    const val EDIT = "edit?id={id}&scan={scan}"
    fun edit(id: Int = -1, scan: Boolean = false) = "edit?id=$id&scan=$scan"

    /** SavedStateHandle key the QR scanner writes its result into on the previous back-stack entry. */
    const val SCAN_RESULT = "scan_result"
}
