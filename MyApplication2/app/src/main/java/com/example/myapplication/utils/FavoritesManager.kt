package com.example.myapplication.utils

import android.content.Context

class FavoritesManager(context: Context) {

    private val prefs =
        context.getSharedPreferences(
            "favorites",
            Context.MODE_PRIVATE
        )

    fun toggle(eventId: Int) {
        val set = getAll().toMutableSet()

        if (set.contains(eventId))
            set.remove(eventId)
        else
            set.add(eventId)

        prefs.edit()
            .putStringSet(
                "ids",
                set.map { it.toString() }.toSet()
            )
            .apply()
    }

    fun isFavorite(eventId: Int): Boolean {
        return getAll().contains(eventId)
    }

    fun getAll(): Set<Int> {
        return prefs
            .getStringSet("ids", emptySet())!!
            .map { it.toInt() }
            .toSet()
    }
}