import { FormEvent, useState } from "react";
import { api } from "../services/api";

type Message = { role: "user" | "assistant"; text: string };
type Reply = { answer: string };

export function AssistantChat() {
  const [open, setOpen] = useState(false);
  const [question, setQuestion] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");

  async function ask(event: FormEvent) {
    event.preventDefault();
    const text = question.trim();
    if (!text || sending) return;
    setQuestion(""); setError(""); setSending(true);
    setMessages((items) => [...items, { role: "user", text }]);
    try {
      const reply = await api<Reply>("/assistant/chat", "POST", { question: text });
      setMessages((items) => [...items, { role: "assistant", text: reply.answer }]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "L'assistant est indisponible.");
    } finally { setSending(false); }
  }

  return <section className="assistant-widget">
    {open && <div className="assistant-panel" role="dialog" aria-label="Assistant d'analyse">
      <header><div><strong>Assistant d’analyse</strong><small>Vos données, en langage naturel</small></div><button className="quiet" onClick={() => setOpen(false)} aria-label="Fermer">×</button></header>
      <div className="assistant-messages" aria-live="polite">
        {!messages.length && <div className="assistant-welcome"><p>Bonjour ! Posez une question sur vos achats, ventes, stocks ou bénéfices.</p><button onClick={() => setQuestion("Quel est mon bénéfice net ce mois-ci ?")}>Bénéfice net ce mois-ci</button><button onClick={() => setQuestion("Quelle quantité ai-je achetée ce mois-ci ?")}>Achats de ce mois</button><button onClick={() => setQuestion("Quel stock reste-t-il ?")}>Stock disponible</button></div>}
        {messages.map((message, index) => <p key={index} className={`assistant-message ${message.role}`}>{message.text}</p>)}
        {sending && <p className="assistant-message assistant">Analyse de vos données…</p>}
      </div>
      {error && <p className="assistant-error" role="alert">{error}</p>}
      <form onSubmit={ask}><input value={question} onChange={(e) => setQuestion(e.target.value)} placeholder="Ex. Quel est mon chiffre net ?" maxLength={1000} /><button disabled={sending}>{sending ? "…" : "Envoyer"}</button></form>
    </div>}
    <button className="assistant-launcher" onClick={() => setOpen(!open)} aria-expanded={open}>✦ Assistant</button>
  </section>;
}
