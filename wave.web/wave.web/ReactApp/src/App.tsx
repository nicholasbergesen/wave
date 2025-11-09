import { useState } from 'react'
import './App.css'
import DocumentPanel from './DocumentPanel'

type Message = {
    role: 'user' | 'assistant'
    content: string
}

function App() {
    const [messages, setMessages] = useState<Message[]>([])
    const [input, setInput] = useState('')
    const [loading, setLoading] = useState(false)
    const [isPanelOpen, setIsPanelOpen] = useState(false)

    const sendMessage = async (): Promise<void> => {
        if (!input.trim()) return

        const newMessages: Message[] = [...messages, { role: 'user', content: input }]
        setMessages(newMessages)
        setInput('')
        setLoading(true)

        try {
            const response = await fetch('https://localhost:7141/api/chat/ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(input),
            })
            const responseMessage = await response.json()
            const assistantMessage = responseMessage.content || 'No response'
            setMessages([...newMessages, { role: 'assistant', content: assistantMessage }])
        } catch (err) {
            setMessages([...newMessages, { role: 'assistant', content: 'Error contacting API.' }])
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="app">
            <header className="header">Wave</header>
            <DocumentPanel isOpen={isPanelOpen} onToggle={() => setIsPanelOpen(!isPanelOpen)} />
            <div className="chat-box">
                {messages.map((msg, i) => (
                    <div key={i} className={`msg ${msg.role}`}>
                        <div className="chat-display">{msg.content}</div>
                    </div>
                ))}
                {loading && <div className="msg assistant">...</div>}
            </div>
            <div className="input-bar">
                <input
                    type="textarea"
                    value={input}
                    placeholder="Type your message..."
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && sendMessage()}
                />
                <button onClick={sendMessage} disabled={loading}>
                    Send
                </button>
            </div>
        </div>
    )
}

export default App
