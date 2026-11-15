import React, { useEffect, useRef, useState } from 'react';
import { Bot, MessageCircle, PackageSearch, Send, X } from 'lucide-react';

const quickPrompts = [
  { label: 'Track an order', prompt: 'Track my latest order' },
  { label: 'Find products', prompt: 'Recommend products' },
  { label: 'Payment FAQ', prompt: 'How do payments and refunds work?' }
];

const ignoredRecommendationWords = new Set([
  'a', 'an', 'and', 'are', 'below', 'budget', 'can', 'dollar', 'dollars', 'find', 'for',
  'in', 'inr', 'less', 'max', 'maximum', 'me', 'of', 'or', 'price', 'product', 'products',
  'recommend', 'recommendation', 'recommendations', 'show', 'some', 'than', 'the', 'to',
  'under', 'what', 'rs', 'rupee', 'rupees'
]);

export default function CustomerServiceChatbot({ products, getOrders }) {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [isReplying, setIsReplying] = useState(false);
  const [messages, setMessages] = useState([
    {
      id: 'welcome',
      role: 'assistant',
      text: 'Hi. I can check your order status, find products in the catalog, and answer store FAQs.'
    }
  ]);
  const messageEndRef = useRef(null);

  useEffect(() => {
    messageEndRef.current?.scrollIntoView({ block: 'nearest' });
  }, [isOpen, isReplying, messages]);

  async function sendMessage(rawText) {
    const text = rawText.trim();
    if (!text || isReplying) return;

    setInput('');
    setMessages((currentMessages) => [
      ...currentMessages,
      { id: createMessageId(), role: 'customer', text }
    ]);
    setIsReplying(true);

    try {
      const response = await getResponse(text, products, getOrders);
      setMessages((currentMessages) => [
        ...currentMessages,
        { id: createMessageId(), role: 'assistant', text: response }
      ]);
    } catch (error) {
      setMessages((currentMessages) => [
        ...currentMessages,
        {
          id: createMessageId(),
          role: 'assistant',
          text: error instanceof Error ? error.message : 'I could not complete that request. Please try again.'
        }
      ]);
    } finally {
      setIsReplying(false);
    }
  }

  function submitMessage(event) {
    event.preventDefault();
    void sendMessage(input);
  }

  return (
    <div className="support-chat">
      {isOpen && (
        <section className="support-chat-panel" aria-label="Customer service chat">
          <header className="support-chat-header">
            <div className="support-chat-title">
              <span className="support-chat-avatar"><Bot size={19} /></span>
              <div>
                <h2>Store assistant</h2>
                <span>Orders, products, and FAQs</span>
              </div>
            </div>
            <button type="button" className="support-chat-close" onClick={() => setIsOpen(false)} aria-label="Close chat">
              <X size={19} />
            </button>
          </header>

          <div className="support-chat-messages" role="log" aria-live="polite" aria-relevant="additions">
            {messages.map((message) => (
              <div className={`support-chat-message ${message.role}`} key={message.id}>
                {message.role === 'assistant' && <span className="support-chat-message-mark"><Bot size={14} /></span>}
                <p>{message.text}</p>
              </div>
            ))}
            {isReplying && <p className="support-chat-typing" role="status">Checking store information...</p>}
            <div ref={messageEndRef} />
          </div>

          <div className="support-chat-prompts" aria-label="Suggested questions">
            {quickPrompts.map((prompt) => (
              <button
                type="button"
                key={prompt.label}
                onClick={() => void sendMessage(prompt.prompt)}
                disabled={isReplying}
              >
                {prompt.label === 'Find products' && <PackageSearch size={14} />}
                {prompt.label}
              </button>
            ))}
          </div>

          <form className="support-chat-form" onSubmit={submitMessage}>
            <label className="visually-hidden" htmlFor="support-chat-input">Message the store assistant</label>
            <input
              id="support-chat-input"
              value={input}
              onChange={(event) => setInput(event.target.value)}
              placeholder="Ask about an order or product"
              autoComplete="off"
              disabled={isReplying}
            />
            <button type="submit" aria-label="Send message" disabled={!input.trim() || isReplying}>
              <Send size={17} />
            </button>
          </form>
          <p className="support-chat-disclaimer">Answers use store records and published demo policies.</p>
        </section>
      )}

      <button
        type="button"
        className="support-chat-launcher"
        aria-expanded={isOpen}
        aria-label={isOpen ? 'Close customer service chat' : 'Open customer service chat'}
        onClick={() => setIsOpen((open) => !open)}
      >
        {isOpen ? <X size={20} /> : <MessageCircle size={20} />}
        <span>{isOpen ? 'Close' : 'Help'}</span>
      </button>
    </div>
  );
}

async function getResponse(text, products, getOrders) {
  const normalized = text.toLocaleLowerCase();

  if (/(track|status|where is|where's).*(order)|(order).*(track|status)/.test(normalized)) {
    return getOrderStatus(text, getOrders);
  }

  if (/\b(recommend|suggest|looking for|find me|show me)\b|\b(products?|catalog|catalogue|shirt|sneakers?|shoes?|tote|bag|lamp)\b/.test(normalized)) {
    return getProductRecommendations(text, products);
  }

  if (/(cancel|refund|money back)/.test(normalized)) {
    return 'Only Pending orders can be cancelled. In this demo, cancellation updates the payment record to Refunded or Cancelled; no funds are moved.';
  }

  if (/(payment|charged|charge|card)/.test(normalized)) {
    return 'Payments in this demo are records only. There is no payment-provider integration and no money is moved.';
  }

  if (/(shipping|shipped|delivery|carrier)/.test(normalized)) {
    return 'I can show the status recorded on your order, but this store is not connected to a shipping carrier and cannot provide live delivery tracking.';
  }

  if (/(help|faq|question)/.test(normalized)) {
    return 'I can look up your order status, suggest products from the current catalog, or explain order cancellation and demo payment behavior.';
  }

  return 'I can help with order status, product recommendations, and questions about cancellation or demo payments. What would you like to check?';
}

async function getOrderStatus(text, getOrders) {
  const orders = await getOrders();
  if (orders.length === 0) return 'I could not find any orders on your account yet.';

  const fullId = text.match(/[0-9a-f]{8}-[0-9a-f-]{27,}/i)?.[0];
  const shortId = !fullId ? text.match(/\b[0-9a-f]{6,8}\b/i)?.[0] : null;
  const order = fullId
    ? orders.find((record) => record.id.toLocaleLowerCase() === fullId.toLocaleLowerCase())
    : shortId
      ? orders.find((record) => record.id.toLocaleLowerCase().startsWith(shortId.toLocaleLowerCase()))
      : [...orders].sort((left, right) => new Date(right.orderedAt) - new Date(left.orderedAt))[0];

  if (!order) return 'I could not match that order number to your account. Check the first 6 to 8 characters and try again.';

  return `Order ${order.id.slice(0, 8)} is currently ${order.status}. Carrier tracking is not available in this store.`;
}

function getProductRecommendations(text, products) {
  const priceLimitMatch = text.match(/\b(?:under|below|less than|max(?:imum)?|budget(?: of)?)\s*(?:₹|rs\.?|inr|\$)?\s*([\d,]+(?:\.\d{1,2})?)\b/i);
  const priceLimit = priceLimitMatch ? Number(priceLimitMatch[1].replace(/,/g, '')) : null;
  const inStockProducts = products.filter((product) => Number(product.stockQuantity) > 0);
  if (inStockProducts.length === 0) return 'There are no in-stock products in the catalog right now.';

  const availableProducts = inStockProducts.filter((product) =>
    priceLimit === null || Number(product.price) <= priceLimit);
  if (availableProducts.length === 0) {
    return `I could not find an in-stock match under ${formatCurrency(priceLimit)}.`;
  }

  const terms = text.toLocaleLowerCase()
    .split(/[^a-z0-9]+/)
    .filter((term) => term.length > 2 && !ignoredRecommendationWords.has(term));
  const rankedProducts = availableProducts
    .map((product) => {
      const searchableText = `${product.name ?? ''} ${product.description ?? ''}`.toLocaleLowerCase();
      const score = terms.reduce((total, term) => total + (searchableText.includes(term) ? 1 : 0), 0);
      return { product, score };
    })
    .filter((entry) => terms.length === 0 || entry.score > 0)
    .sort((left, right) => right.score - left.score ||
      (priceLimit === null ? 0 : Number(left.product.price) - Number(right.product.price)) ||
      left.product.name.localeCompare(right.product.name))
    .slice(0, 3)
    .map(({ product }) => `${product.name} (${formatCurrency(product.price)})`);

  if (rankedProducts.length === 0) return 'I could not find an in-stock match. Try a product name or a word from its description.';
  return `Here are in-stock catalog matches:\n${rankedProducts.map((product) => `• ${product}`).join('\n')}`;
}

function formatCurrency(value) {
  return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(Number(value) || 0);
}

function createMessageId() {
  return `${Date.now()}-${Math.random()}`;
}