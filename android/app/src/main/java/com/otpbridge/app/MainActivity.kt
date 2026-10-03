package com.otpbridge.app

import android.Manifest
import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.codescanner.GmsBarcodeScannerOptions
import com.google.mlkit.vision.codescanner.GmsBarcodeScanning
import kotlin.concurrent.thread
import kotlin.random.Random

class MainActivity : Activity() {
    private lateinit var pairText: TextView
    private lateinit var statusText: TextView
    private lateinit var codeInput: EditText

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val pad = (16 * resources.displayMetrics.density).toInt()
        pairText = TextView(this).apply { textSize = 20f }
        statusText = TextView(this).apply { setPadding(0, pad / 2, 0, pad) }
        codeInput = EditText(this).apply {
            hint = "Or paste pairing code (OTPB1|…)"
            isSingleLine = true
        }
        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(pad, pad, pad, pad)
            addView(pairText)
            addView(statusText)
            addView(button("Scan QR from PC") { scanQr() })
            addView(codeInput)
            addView(button("Pair with pasted code") { pair(codeInput.text.toString()) })
            addView(button("Send test OTP") { sendTest() })
            addView(button("Allow running in background") { askBatteryExemption() })
            addView(button("Unpair") { PairStore.clear(this@MainActivity); refresh() })
        }
        setContentView(ScrollView(this).apply { addView(root) })

        if (!hasSmsPermission()) requestPermissions(arrayOf(Manifest.permission.RECEIVE_SMS), 1)
    }

    override fun onResume() {
        super.onResume()
        refresh()
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        refresh()
    }

    private fun refresh() {
        val pair = PairStore.load(this)
        val battery = getSystemService(PowerManager::class.java).isIgnoringBatteryOptimizations(packageName)
        pairText.text = if (pair != null) "Paired with ${pair.name}" else "Not paired. Scan the QR shown by the PC app."
        statusText.text = buildString {
            appendLine("SMS permission: " + if (hasSmsPermission()) "✓" else "✗ needed (Settings › Apps › OTP Bridge)")
            appendLine("Background: " + if (battery) "✓" else "✗ recommended")
            append(PairStore.status(this@MainActivity))
        }
    }

    private fun scanQr() {
        val options = GmsBarcodeScannerOptions.Builder().setBarcodeFormats(Barcode.FORMAT_QR_CODE).build()
        GmsBarcodeScanning.getClient(this, options).startScan()
            .addOnSuccessListener { pair(it.rawValue ?: "") }
            .addOnFailureListener { toast("Scanner unavailable (${it.message}). Paste the code instead.") }
    }

    private fun pair(code: String) {
        val info = PairInfo.decode(code) ?: return toast("Invalid pairing code")
        PairStore.save(this, info)
        PairStore.setStatus(this, "Paired. Tap 'Send test OTP'.")
        refresh()
    }

    private fun sendTest() {
        toast("Sending…")
        val otp = Random.nextInt(100000, 999999).toString()
        thread {
            Relay.send(this, "test", otp, "OTP Bridge test")
            runOnUiThread { refresh() }
        }
    }

    private fun askBatteryExemption() {
        startActivity(Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS, Uri.parse("package:$packageName")))
    }

    private fun hasSmsPermission() =
        checkSelfPermission(Manifest.permission.RECEIVE_SMS) == PackageManager.PERMISSION_GRANTED

    private fun button(label: String, onClick: () -> Unit) =
        Button(this).apply { text = label; setOnClickListener { onClick() } }

    private fun toast(msg: String) = Toast.makeText(this, msg, Toast.LENGTH_LONG).show()
}
