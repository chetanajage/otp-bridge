package com.otpbridge.app

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.provider.Telephony
import kotlin.concurrent.thread

class SmsReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action != Telephony.Sms.Intents.SMS_RECEIVED_ACTION) return
        val parts = Telephony.Sms.Intents.getMessagesFromIntent(intent) ?: return
        val from = parts.firstOrNull()?.displayOriginatingAddress ?: ""
        val body = parts.joinToString("") { it.displayMessageBody ?: "" }
        val otp = OtpExtractor.extract(body) ?: return
        if (PairStore.load(context) == null) return

        val pending = goAsync()
        val appContext = context.applicationContext
        thread {
            try {
                if (!Relay.send(appContext, "otp", otp, from)) {
                    Thread.sleep(2000) // Wi-Fi may still be waking up
                    Relay.send(appContext, "otp", otp, from)
                }
            } finally {
                pending.finish()
            }
        }
    }
}
