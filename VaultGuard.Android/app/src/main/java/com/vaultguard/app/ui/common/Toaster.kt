package com.vaultguard.app.ui.common

import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

/** App-wide toast/snackbar bus. Emit messages from anywhere; a host in MainActivity shows them. */
@Singleton
class Toaster @Inject constructor() {
    private val _messages = MutableSharedFlow<String>(extraBufferCapacity = 4)
    val messages: SharedFlow<String> = _messages.asSharedFlow()

    fun show(message: String) { _messages.tryEmit(message) }
}
