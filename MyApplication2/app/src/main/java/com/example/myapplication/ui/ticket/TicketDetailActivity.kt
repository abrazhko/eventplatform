package com.example.myapplication.ui.ticket

import android.graphics.Bitmap
import android.os.Build
import android.os.Bundle
import android.widget.ImageView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.example.myapplication.R
import com.journeyapps.barcodescanner.BarcodeEncoder

class TicketDetailActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_ticket_detail)

        val tvTitle = findViewById<TextView>(R.id.tvTitle)
        val tvDate = findViewById<TextView>(R.id.tvDate)
        val tvLocation = findViewById<TextView>(R.id.tvLocation)
        val ivQrCode = findViewById<ImageView>(R.id.ivQrCode)

        val tvStatus = findViewById<TextView>(R.id.tvStatus)
        val status = intent.getIntExtra("status", 0)

        tvStatus.text = when(status) {
            1 -> "🟢 Активен"
            2 -> "⚪ Использован"
            else -> "🔴 Отменён"
        }

        tvStatus.setTextColor(
            when(status) {
                1 -> 0xFF4CAF50.toInt() // зеленый
                2 -> 0xFF9E9E9E.toInt() // серый
                else -> 0xFFE53935.toInt() // красный
            }
        )

        tvTitle.text = intent.getStringExtra("title")
        val dateStr = intent.getStringExtra("date") ?: ""
        tvDate.text = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            com.example.myapplication.utils.DateUtils.formatDate(dateStr)
        } else {
            dateStr
        }
        val locationText = intent.getStringExtra("location") ?: ""
        tvLocation.text = "📍$locationText"

        val qrText = intent.getStringExtra("qrCode") ?: ""

        try {
            val barcodeEncoder = BarcodeEncoder()
            val bitmap: Bitmap = barcodeEncoder.encodeBitmap(
                qrText,
                com.google.zxing.BarcodeFormat.QR_CODE,
                900,
                900
            )
            ivQrCode.setImageBitmap(bitmap)
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
}