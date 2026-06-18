using System.Net.Http;
using System.Text.Json;
using BlitzText.Windows.Models;

namespace BlitzText.Windows.Services;

public static class UserErrorFormatter
{
    public static string Format(Exception exception, AppLanguage language)
    {
        var message = exception.Message;
        var lower = message.ToLowerInvariant();
        var english = language == AppLanguage.English;

        if (exception is HttpRequestException || lower.Contains("connection refused") || lower.Contains("actively refused"))
        {
            return english
                ? "The provider is not reachable. Check the URL, internet connection, firewall, or whether the local server is running."
                : "Der Anbieter ist nicht erreichbar. Pruefe URL, Internetverbindung, Firewall oder ob der lokale Server laeuft.";
        }

        if (exception is TaskCanceledException || lower.Contains("timeout") || lower.Contains("timed out"))
        {
            return english
                ? "The request timed out. The provider may be busy, offline, or the model may still be loading."
                : "Die Anfrage hat zu lange gedauert. Der Anbieter ist eventuell beschaeftigt, offline oder das Modell laedt noch.";
        }

        if (lower.Contains("api key is missing"))
        {
            return english
                ? "An API key is missing for the selected provider. Add it in the Provider tab."
                : "Fuer den gewaehlten Anbieter fehlt ein API-Key. Trage ihn im Provider-Tab ein.";
        }

        if (lower.Contains("401") || lower.Contains("unauthorized"))
        {
            return english
                ? "The API key was rejected. Check whether the key is correct and still active."
                : "Der API-Key wurde abgelehnt. Pruefe, ob der Schluessel korrekt und noch aktiv ist.";
        }

        if (lower.Contains("403") || lower.Contains("forbidden"))
        {
            return english
                ? "Access was denied. Check provider permissions, account status, or model access."
                : "Der Zugriff wurde verweigert. Pruefe Berechtigungen, Kontostatus oder Modellzugriff.";
        }

        if (lower.Contains("404") || lower.Contains("not found"))
        {
            return english
                ? "The provider endpoint or model was not found. Check URL and model name."
                : "Anbieter-Endpunkt oder Modell wurde nicht gefunden. Pruefe URL und Modellnamen.";
        }

        if (lower.Contains("429") || lower.Contains("rate limit"))
        {
            return english
                ? "The provider rate limit was reached. Wait briefly or check your quota."
                : "Das Anbieter-Limit wurde erreicht. Warte kurz oder pruefe dein Kontingent.";
        }

        if (lower.Contains("ollama"))
        {
            return english
                ? $"Ollama reported a problem. Check whether Ollama is running and the selected model exists. Details: {message}"
                : $"Ollama meldet ein Problem. Pruefe, ob Ollama laeuft und das gewaehlte Modell vorhanden ist. Details: {message}";
        }

        if (lower.Contains("whisper"))
        {
            return english
                ? $"Whisper could not be used. Check the executable path, model file, and timeout. Details: {message}"
                : $"Whisper konnte nicht verwendet werden. Pruefe EXE-Pfad, Modelldatei und Timeout. Details: {message}";
        }

        if (exception is JsonException)
        {
            return english
                ? "The provider returned an unexpected response. Try again or check the provider settings."
                : "Der Anbieter hat eine unerwartete Antwort geliefert. Versuche es erneut oder pruefe die Einstellungen.";
        }

        return message;
    }
}
