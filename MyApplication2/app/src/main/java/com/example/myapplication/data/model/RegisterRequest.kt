package com.example.myapplication.data.model

data class RegisterRequest(
    val fullName: String,
    val email: String,
    val password: String
)