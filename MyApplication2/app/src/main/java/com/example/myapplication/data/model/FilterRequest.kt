package com.example.myapplication.data.model

data class FilterRequest(

    val search: String? = null,

    val category: String? = null,

    val dateFrom: String? = null,

    val dateTo: String? = null,

    val priceMin: Double? = null,

    val priceMax: Double? = null,

    val onlyFree: Boolean? = null
)