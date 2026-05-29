package com.example.myapplication.data.model

data class LoginResponse(

    val token: String,

    val id: Int,

    val fullName: String,

    val email: String
)