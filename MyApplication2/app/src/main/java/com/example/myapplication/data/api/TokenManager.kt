package com.example.myapplication.data.api

import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey

class TokenManager(context: Context) {

    private val prefs =
        EncryptedSharedPreferences.create(

            context,

            "auth",

            MasterKey.Builder(context)

                .setKeyScheme(

                    MasterKey.KeyScheme.AES256_GCM

                )

                .build(),

            EncryptedSharedPreferences
                .PrefKeyEncryptionScheme
                .AES256_SIV,

            EncryptedSharedPreferences
                .PrefValueEncryptionScheme
                .AES256_GCM
        )


    fun saveToken(token: String) {

        prefs.edit()

            .putString("token", token)

            .apply()
    }


    fun getToken(): String? {

        return prefs.getString(

            "token",

            null
        )
    }
    fun clearToken() {
        prefs.edit().remove("token").apply()
    }
}