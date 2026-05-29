package com.example.myapplication.data.model

data class Event(
    val id: Int,
    val title: String,
    val description: String,
    val startDate: String,
    val location: String,
    val price: Double,
    val category: String,
    val ticketsLeft: Int,
    val imageUrl: String?
)