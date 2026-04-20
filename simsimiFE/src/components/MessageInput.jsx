import React, { useState } from 'react';
import '../styles/MessageInput.css';

const RUDENESS_LEVELS = [
  { value: 'Polite', label: 'Lich su', accent: 'Tinh te' },
  { value: 'Neutral', label: 'Trung tinh', accent: 'Can bang' },
  { value: 'Casual', label: 'Thoai mai', accent: 'Tu nhien' },
  { value: 'Sarcastic', label: 'Mia mai', accent: 'Cham biem' },
  { value: 'Rude', label: 'Tho', accent: 'Gat' },
];

export default function MessageInput({
  onSendMessage,
  disabled,
  rudenessLevel,
  onRudenessLevelChange,
}) {
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);

  const handleSend = async () => {
    if (!message.trim() || disabled || sending) return;

    setSending(true);
    try {
      await onSendMessage(message, rudenessLevel);
      setMessage('');
    } catch (err) {
      console.error('Error sending message:', err);
    } finally {
      setSending(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="message-input-container">
      <div className="message-input-shell">
        <textarea
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Nhap tin nhan... (Shift+Enter de xuong dong)"
          disabled={disabled || sending}
          rows="1"
        />

        <div className="composer-toolbar">
          <div className="rudeness-selector">
            {RUDENESS_LEVELS.map((level) => (
              <button
                key={level.value}
                className={`rudeness-btn ${rudenessLevel === level.value ? 'active' : ''}`}
                onClick={() => onRudenessLevelChange(level.value)}
                title={level.label}
                disabled={disabled || sending}
              >
                <span className="rudeness-chip-main">{level.label}</span>
                <span className="rudeness-chip-sub">{level.accent}</span>
              </button>
            ))}
          </div>

          <button
            onClick={handleSend}
            disabled={!message.trim() || disabled || sending}
            className="send-btn"
          >
            {sending ? 'Dang gui...' : 'Gui'}
          </button>
        </div>
      </div>

      <div className="rudeness-label">
        Phong cach AI: {RUDENESS_LEVELS.find((level) => level.value === rudenessLevel)?.label || 'Trung tinh'}
      </div>
    </div>
  );
}
