package com.vaultguard.app.ui.navigation

object Routes {
    const val SETUP = "setup"
    const val UNLOCK = "unlock"
    const val HOME = "home"
    const val SETTINGS = "settings"
    const val SCAN = "scan"
    const val VAULTS = "vaults"
    const val CATEGORIES = "categories"
    const val SECURITY = "security"
    const val IMPORT = "import"
    const val PROFILE = "profile"
    const val ABOUT = "about"

    const val DETAIL = "detail/{id}"
    fun detail(id: Int) = "detail/$id"

    // id = -1 means "new item"
    const val EDIT = "edit?id={id}"
    fun edit(id: Int = -1) = "edit?id=$id"

    /** SavedStateHandle key the QR scanner writes its result into on the previous back-stack entry. */
    const val SCAN_RESULT = "scan_result"
}
