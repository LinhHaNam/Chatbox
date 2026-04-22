import React, { useEffect, useRef } from 'react';
import MessageInput from './MessageInput';
import '../styles/ChatWindow.css';
import { formatVietnamDate, formatVietnamTime } from '../utils/dateTime';

const getMessageTimestamp = (message) => {
  const value = message?.createdAt;
  const timestamp = value ? new Date(value).getTime() : Number.NaN;
  return Number.isNaN(timestamp) ? 0 : timestamp;
};

const getSortedMessages = (messages = []) =>
  [...messages]
    .map((message, index) => ({ message, index }))
    .sort((left, right) => {
      const timestampDiff = getMessageTimestamp(left.message) - getMessageTimestamp(right.message);
      if (timestampDiff !== 0) {
        return timestampDiff;
      }

      return left.index - right.index;
    })
    .map(({ message }) => message);

export default function ChatWindow({
  session,
  onSendMessage,
  onRudenessLevelChange,
  loading,
  onOpenSidebar,
  showSidebarButton,
}) {
  const messagesEndRef = useRef(null);
  const sessionDate = session?.lastActiveAt || session?.startedAt;
  const sortedMessages = getSortedMessages(session?.messages || []);
  const hasMessages = sortedMessages.length > 0;

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' });
  }, [sortedMessages]);

  return (
    <div className="chat-window">
      <div className="chat-header">
        <div className="chat-header-left">
          {showSidebarButton && (
            <button className="chat-nav-btn" onClick={onOpenSidebar} title="Mo danh sach cuoc tro chuyen">
              ☰
            </button>
          )}
          <div className="chat-header-copy">
            <h2>SimsimiChat</h2>
            <p>Gio Viet Nam (GMT+7)</p>
          </div>
        </div>

        {sessionDate && (
          <span className="session-date">
            {formatVietnamDate(sessionDate, {
              hour: '2-digit',
              minute: '2-digit',
              day: '2-digit',
              month: '2-digit',
              year: 'numeric',
            })}
          </span>
        )}
      </div>

      <div className={`messages-container ${hasMessages ? 'has-messages' : 'is-empty'}`}>
        {hasMessages ? (
          <div className="messages-list">
            {sortedMessages.map((message) => (
              <div
                key={message.id}
                className={`message ${message.senderType === 'User' ? 'user-message' : 'bot-message'}`}
              >
                <div className="message-content">
                  <p>{message.content}</p>
                  <span className="message-time">{formatVietnamTime(message.createdAt)}</span>
                </div>
              </div>
            ))}
            <div ref={messagesEndRef} />
          </div>
        ) : (
          <div className="chat-hero">
            <p className="chat-hero-kicker">Xin chao</p>
            <h1 className="chat-hero-title">Hom nay ban muon SimsimiChat giup gi?</h1>
            <p className="chat-hero-subtitle">
              Chon mot cuoc tro chuyen o ben trai hoac bat dau bang mot tin nhan moi.
            </p>
          </div>
        )}
      </div>

      <MessageInput
        onSendMessage={onSendMessage}
        onRudenessLevelChange={onRudenessLevelChange}
        rudenessLevel={session?.defaultRudenessLevel || 'Neutral'}
        disabled={loading}
      />
    </div>
  );
}
