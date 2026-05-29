package com.example.myapplication.utils

import android.os.Build
import androidx.annotation.RequiresApi
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter

object DateUtils {

    @RequiresApi(Build.VERSION_CODES.O)
    fun formatDate(dateStr: String?): String {

        if (dateStr.isNullOrBlank())
            return ""

        return try {

            val date =
                OffsetDateTime.parse(dateStr)
                    .plusHours(3)

            date.format(
                DateTimeFormatter.ofPattern(
                    "dd.MM.yyyy HH:mm"
                )
            )

        } catch (e: Exception) {

            dateStr
        }
    }
}