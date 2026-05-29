package com.example.myapplication.data.model

data class Ticket(

    val id: Int,

    val qrCode: String,

    val status: Int,

    val purchasedAt: String,

    val eventTitle: String,

    val startDate: String,

    val location: String
)