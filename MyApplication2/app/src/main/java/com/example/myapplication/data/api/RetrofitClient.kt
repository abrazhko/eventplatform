package com.example.myapplication.data.api

import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val BASE_URL =
        "http://10.0.2.2:8080/api/"

    fun create(token: String?): ApiService {

        val client =
            OkHttpClient.Builder()

        if (token != null) {

            client.addInterceptor {

                val request =
                    it.request()
                        .newBuilder()
                        .addHeader(
                            "Authorization",
                            "Bearer $token"
                        )
                        .build()

                it.proceed(request)
            }
        }

        return Retrofit.Builder()

            .baseUrl(BASE_URL)

            .client(client.build())

            .addConverterFactory(
                GsonConverterFactory.create()
            )

            .build()

            .create(ApiService::class.java)
    }
}