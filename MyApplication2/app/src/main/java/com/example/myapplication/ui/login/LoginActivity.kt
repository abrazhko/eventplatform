package com.example.myapplication.ui.login
import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.data.model.LoginRequest
import com.example.myapplication.ui.events.EventsActivity
import com.example.myapplication.ui.register.RegisterActivity

import kotlinx.coroutines.*

class LoginActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_login)

        val emailEdit = findViewById<EditText>(R.id.emailEdit)
        val passEdit = findViewById<EditText>(R.id.passwordEdit)
        val loginBtn = findViewById<Button>(R.id.loginButton)
        val registerBtn = findViewById<Button>(R.id.registerButton)
        //val forgotPassword = findViewById<TextView>(R.id.forgotPassword)

        val tokenManager = TokenManager(this)
        val api = RetrofitClient.create(null)

        loginBtn.setOnClickListener {
            val email = emailEdit.text.toString().trim()
            val password = passEdit.text.toString()

            // Валидация
            if(email.isEmpty() || !android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                emailEdit.error = "Введите корректный email"
                return@setOnClickListener
            }

            if(password.isEmpty()) {
                passEdit.error = "Введите пароль"
                return@setOnClickListener
            }

            // Корутин для запроса на backend
            CoroutineScope(Dispatchers.IO).launch {
                try {
                    val response = api.login(LoginRequest(email, password))
                    tokenManager.saveToken(response.token)

                    withContext(Dispatchers.Main) {
                        startActivity(Intent(this@LoginActivity, EventsActivity::class.java))
                        finish()
                    }
                } catch (e: Exception) {
                    withContext(Dispatchers.Main) {
                        passEdit.error = "Неверный email или пароль"
                    }
                }
            }
        }

        registerBtn.setOnClickListener {
            startActivity(Intent(this, RegisterActivity::class.java))
        }

        //forgotPassword.setOnClickListener {
            // Можно открыть экран восстановления пароля потом мб сделаю
            // startActivity(Intent(this, ForgotPasswordActivity::class.java))
        //}
    }
}