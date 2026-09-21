import React, { useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  CheckCircle2,
  CreditCard,
  Loader2,
  LogOut,
  Minus,
  Package,
  Plus,
  RefreshCw,
  Save,
  ShoppingCart,
  Trash2,
  User
} from 'lucide-react';
import './styles.css';

const AUTH_STORAGE_KEY = 'ecommerce_auth';
const LOGIN_API = '/api/users/api/users/login';
const USERS_API = '/api/users/api/users';
const PRODUCT_API = '/api/products/api/products';
const CART_API = '/api/cart/api/carts';
const ORDERS_API = '/api/orders/api/orders';
const PAYMENTS_API = '/api/payments/api/payments';

const starterProducts = [
  {
    name: 'Oxford Shirt',
    description: 'Clean everyday shirt with a structured collar.',
    price: 42.0,
    stockQuantity: 80
  },
  {
    name: 'Canvas Tote',
    description: 'Durable carry bag for daily shopping and work.',
    price: 24.5,
    stockQuantity: 120
  },
  {
    name: 'Running Sneakers',
    description: 'Lightweight shoes built for comfort and movement.',
    price: 88.75,
    stockQuantity: 45
  },
  {
    name: 'Desk Lamp',
    description: 'Adjustable blue-white task lamp for focused work.',
    price: 36.25,
    stockQuantity: 60
  }
];

function App() {
  const [auth, setAuth] = useState(() => readStoredAuth());
  const [loginForm, setLoginForm] = useState({ name: '', email: '', password: '' });
  const [isCreatingAccount, setIsCreatingAccount] = useState(false);
  const [isLoggingIn, setIsLoggingIn] = useState(false);
  const [loginError, setLoginError] = useState('');
  const [products, setProducts] = useState([]);
  const [productForm, setProductForm] = useState({
    name: '',
    description: '',
    price: '',
    stockQuantity: ''
  });
  const [cart, setCart] = useState([]);
  const [isLoadingProducts, setIsLoadingProducts] = useState(false);
  const [isSeeding, setIsSeeding] = useState(false);
  const [isSavingProduct, setIsSavingProduct] = useState(false);
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [notice, setNotice] = useState('');
  const [checkoutResult, setCheckoutResult] = useState(null);
  const [activePanel, setActivePanel] = useState(null);

  useEffect(() => {
    if (auth) {
      loadProducts();
    }
  }, [auth]);

  const cartTotal = useMemo(
    () => cart.reduce((total, item) => total + item.product.price * item.quantity, 0),
    [cart]
  );

  const cartItemCount = useMemo(
    () => cart.reduce((total, item) => total + item.quantity, 0),
    [cart]
  );

  const isAdmin = auth?.role?.toLowerCase() === 'admin';

  async function loadProducts() {
    if (!auth) {
      return;
    }

    setIsLoadingProducts(true);
    setNotice('');

    try {
      const records = await apiRequest(PRODUCT_API, {}, auth.accessToken);
      setProducts(Array.isArray(records) ? records : []);
    } catch (error) {
      setNotice(getErrorMessage(error, 'Unable to load product catalogue.'));
    } finally {
      setIsLoadingProducts(false);
    }
  }

  async function seedCatalogue() {
    setIsSeeding(true);
    setNotice('');

    try {
      await Promise.all(
        starterProducts.map((product) =>
          apiRequest(PRODUCT_API, {
            method: 'POST',
            body: product
          }, auth.accessToken)
        )
      );
      await loadProducts();
      setNotice('Catalogue products were created.');
    } catch (error) {
      setNotice(getErrorMessage(error, 'Unable to create catalogue products.'));
    } finally {
      setIsSeeding(false);
    }
  }

  async function createProduct(event) {
    event.preventDefault();
    setIsSavingProduct(true);
    setNotice('');

    try {
      await apiRequest(PRODUCT_API, {
        method: 'POST',
        body: {
          name: productForm.name,
          description: productForm.description,
          price: Number(productForm.price),
          stockQuantity: Number(productForm.stockQuantity)
        }
      }, auth.accessToken);

      setProductForm({ name: '', description: '', price: '', stockQuantity: '' });
      await loadProducts();
      setNotice('Product was added to the catalogue.');
    } catch (error) {
      setNotice(getErrorMessage(error, 'Unable to add product.'));
    } finally {
      setIsSavingProduct(false);
    }
  }

  function addToCart(product) {
    setCart((items) => {
      const existing = items.find((item) => item.product.id === product.id);

      if (existing) {
        return items.map((item) =>
          item.product.id === product.id ? { ...item, quantity: item.quantity + 1 } : item
        );
      }

      return [...items, { product, quantity: 1 }];
    });
    setCheckoutResult(null);
  }

  function updateQuantity(productId, change) {
    setCart((items) =>
      items
        .map((item) =>
          item.product.id === productId
            ? { ...item, quantity: Math.max(0, item.quantity + change) }
            : item
        )
        .filter((item) => item.quantity > 0)
    );
  }

  function removeItem(productId) {
    setCart((items) => items.filter((item) => item.product.id !== productId));
  }

  function togglePanel(panelName) {
    setActivePanel((currentPanel) => (currentPanel === panelName ? null : panelName));
  }

  async function login(event) {
    event.preventDefault();
    setIsLoggingIn(true);
    setLoginError('');

    try {
      if (isCreatingAccount) {
        await createAccount();
      }

      const authResponse = await apiRequest(LOGIN_API, {
        method: 'POST',
        body: {
          email: loginForm.email,
          password: loginForm.password
        }
      });

      localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(authResponse));
      setAuth(authResponse);
      setLoginForm({ name: '', email: '', password: '' });
    } catch (error) {
      const message = getErrorMessage(error, 'Login failed.');
      if (!isCreatingAccount && message === 'Unauthorized') {
        setIsCreatingAccount(true);
        setLoginError('No account matched those credentials. Create a new account below.');
      } else {
        setLoginError(message);
      }
    } finally {
      setIsLoggingIn(false);
    }
  }

  async function createAccount() {
    await apiRequest(USERS_API, {
      method: 'POST',
      body: {
        name: loginForm.name || loginForm.email.split('@')[0],
        email: loginForm.email,
        passwordHash: loginForm.password,
        role: 'Customer'
      }
    });
  }

  function logout() {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    setAuth(null);
    setProducts([]);
    setCart([]);
    setCheckoutResult(null);
    setActivePanel(null);
    setNotice('');
  }

  async function checkout() {
    if (cart.length === 0) {
      setNotice('Add at least one product before checkout.');
      return;
    }

    setIsCheckingOut(true);
    setNotice('');
    setCheckoutResult(null);

    const lineItems = cart.map((item) => ({
      productId: item.product.id,
      quantity: item.quantity,
      unitPrice: item.product.price
    }));

    try {
      const createdCart = await apiRequest(CART_API, {
        method: 'POST',
        body: {
          userId: auth.userId,
          status: 'Active',
          cartItems: lineItems
        }
      }, auth.accessToken);

      if (createdCart?.id) {
        await apiRequest(`${CART_API}/${createdCart.id}/checkout`, {
          method: 'POST',
          body: {
            userId: auth.userId,
            paymentMethod: 'Card'
          }
        }, auth.accessToken);
      }

      const order = await apiRequest(ORDERS_API, {
        method: 'POST',
        body: {
          userId: auth.userId,
          totalAmount: roundMoney(cartTotal),
          status: 'Pending',
          orderItems: lineItems
        }
      }, auth.accessToken);

      const payment = await apiRequest(PAYMENTS_API, {
        method: 'POST',
        body: {
          orderId: order.id,
          userId: auth.userId,
          amount: roundMoney(cartTotal),
          currency: 'USD',
          paymentMethod: 'Card',
          status: 'Completed'
        }
      }, auth.accessToken);

      setCheckoutResult({ cart: createdCart, order, payment });
      setCart([]);
      setActivePanel('cart');
      setNotice('Checkout completed and payment was recorded.');
    } catch (error) {
      setNotice(getErrorMessage(error, 'Checkout failed.'));
    } finally {
      setIsCheckingOut(false);
    }
  }

  if (!auth) {
    return (
      <main className="login-shell">
        <section className="login-panel" aria-labelledby="login-title">
          <div className="brand login-brand">
            <div className="brand-mark">EC</div>
            <div>
              <h1 id="login-title">Ecommerce Store</h1>
              <p>{isCreatingAccount ? 'Create an account to continue' : 'Sign in to view the catalogue'}</p>
            </div>
          </div>

          <form className="login-form" onSubmit={login}>
            {isCreatingAccount && (
              <label>
                <span>Name</span>
                <input
                  type="text"
                  value={loginForm.name}
                  onChange={(event) => setLoginForm((form) => ({ ...form, name: event.target.value }))}
                />
              </label>
            )}
            <label>
              <span>Email</span>
              <input
                type="email"
                value={loginForm.email}
                onChange={(event) => setLoginForm((form) => ({ ...form, email: event.target.value }))}
                required
              />
            </label>
            <label>
              <span>Password</span>
              <input
                type="password"
                value={loginForm.password}
                onChange={(event) => setLoginForm((form) => ({ ...form, password: event.target.value }))}
                required
              />
            </label>

            {loginError && <p className="login-error">{loginError}</p>}

            <button className="login-button" type="submit" disabled={isLoggingIn}>
              {isLoggingIn ? <Loader2 className="spin" size={18} /> : <User size={18} />}
              {isCreatingAccount ? 'Create account and sign in' : 'Sign in'}
            </button>

            <button
              className="link-button"
              type="button"
              onClick={() => {
                setIsCreatingAccount((value) => !value);
                setLoginError('');
              }}
              disabled={isLoggingIn}
            >
              {isCreatingAccount ? 'Use existing account' : 'New user? Create account'}
            </button>
          </form>
        </section>
      </main>
    );
  }

  if (isAdmin) {
    return (
      <main className="store-shell">
        <header className="topbar">
          <div className="brand">
            <div className="brand-mark">EC</div>
            <div>
              <h1>Admin Dashboard</h1>
              <p>Catalogue management</p>
            </div>
          </div>

          <nav className="header-actions" aria-label="Admin actions">
            <button type="button" className="nav-button" onClick={loadProducts} disabled={isLoadingProducts}>
              {isLoadingProducts ? <Loader2 className="spin" size={18} /> : <RefreshCw size={18} />}
              <span>Refresh</span>
            </button>
            <button className="icon-button" type="button" onClick={logout} aria-label="Sign out">
              <LogOut size={20} />
            </button>
          </nav>
        </header>

        {notice && <p className="notice">{notice}</p>}

        <section className="admin-grid">
          <form className="admin-panel" onSubmit={createProduct} aria-labelledby="add-product-title">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">Products</p>
                <h2 id="add-product-title">Add Product</h2>
              </div>
              <Package size={22} />
            </div>

            <div className="admin-form">
              <label>
                <span>Name</span>
                <input
                  type="text"
                  value={productForm.name}
                  onChange={(event) => setProductForm((form) => ({ ...form, name: event.target.value }))}
                  required
                />
              </label>
              <label>
                <span>Description</span>
                <textarea
                  value={productForm.description}
                  onChange={(event) => setProductForm((form) => ({ ...form, description: event.target.value }))}
                  required
                />
              </label>
              <div className="admin-form-row">
                <label>
                  <span>Price</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={productForm.price}
                    onChange={(event) => setProductForm((form) => ({ ...form, price: event.target.value }))}
                    required
                  />
                </label>
                <label>
                  <span>Stock</span>
                  <input
                    type="number"
                    min="0"
                    step="1"
                    value={productForm.stockQuantity}
                    onChange={(event) => setProductForm((form) => ({ ...form, stockQuantity: event.target.value }))}
                    required
                  />
                </label>
              </div>
            </div>

            <button className="login-button" type="submit" disabled={isSavingProduct}>
              {isSavingProduct ? <Loader2 className="spin" size={18} /> : <Save size={18} />}
              Add product
            </button>
          </form>

          <section className="admin-panel" aria-labelledby="admin-catalogue-title">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">Catalogue</p>
                <h2 id="admin-catalogue-title">Current Products</h2>
              </div>
              <span className="catalogue-count">{products.length} products</span>
            </div>

            {isLoadingProducts ? (
              <div className="empty-state compact-empty">
                <Loader2 className="spin" size={24} />
                <p>Loading products</p>
              </div>
            ) : products.length === 0 ? (
              <div className="empty-state compact-empty">
                <Package size={28} />
                <h3>No products found</h3>
                <p>Create starter products or add a product manually.</p>
                <button type="button" onClick={seedCatalogue} disabled={isSeeding}>
                  {isSeeding ? <Loader2 className="spin" size={17} /> : <Plus size={17} />}
                  Create starter catalogue
                </button>
              </div>
            ) : (
              <div className="admin-product-list">
                {products.map((product) => (
                  <article className="admin-product-row" key={product.id}>
                    <div>
                      <strong>{product.name}</strong>
                      <span>{product.description || 'No description available.'}</span>
                    </div>
                    <div>
                      <strong>{formatMoney(product.price)}</strong>
                      <span>{product.stockQuantity ?? 0} in stock</span>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>
        </section>
      </main>
    );
  }

  return (
    <main className="store-shell">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">EC</div>
          <div>
            <h1>Ecommerce Store</h1>
            <p>Product catalogue</p>
          </div>
        </div>

        <nav className="header-actions" aria-label="Store actions">
          <button type="button" className="nav-button" onClick={loadProducts} disabled={isLoadingProducts}>
            {isLoadingProducts ? <Loader2 className="spin" size={18} /> : <RefreshCw size={18} />}
            <span>Refresh</span>
          </button>
          <button
            type="button"
            className={activePanel === 'cart' ? 'icon-button active' : 'icon-button'}
            onClick={() => togglePanel('cart')}
            aria-label="Open cart"
          >
            <ShoppingCart size={20} />
            {cartItemCount > 0 && <span className="count-badge">{cartItemCount}</span>}
          </button>
          <button
            type="button"
            className={activePanel === 'profile' ? 'icon-button active' : 'icon-button'}
            onClick={() => togglePanel('profile')}
            aria-label="Open profile"
          >
            <User size={20} />
          </button>
        </nav>
      </header>

      {notice && <p className="notice">{notice}</p>}

      <section className="catalogue" aria-labelledby="catalogue-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Shop</p>
            <h2 id="catalogue-title">Product Catalogue</h2>
          </div>
          <span className="catalogue-count">{products.length} products</span>
        </div>

        {isLoadingProducts ? (
          <div className="empty-state">
            <Loader2 className="spin" size={24} />
            <p>Loading products</p>
          </div>
        ) : products.length === 0 ? (
          <div className="empty-state">
            <Package size={28} />
            <h3>No products found</h3>
            <p>Create starter products to see the catalogue and test the checkout flow.</p>
            <button type="button" onClick={seedCatalogue} disabled={isSeeding}>
              {isSeeding ? <Loader2 className="spin" size={17} /> : <Plus size={17} />}
              Create starter catalogue
            </button>
          </div>
        ) : (
          <div className="product-grid">
            {products.map((product) => (
              <article className="product-card" key={product.id}>
                <div className="product-image">
                  <Package size={34} />
                </div>
                <div className="product-body">
                  <div>
                    <h3>{product.name}</h3>
                    <p>{product.description || 'No description available.'}</p>
                  </div>
                  <div className="product-meta">
                    <strong>{formatMoney(product.price)}</strong>
                    <span>{product.stockQuantity ?? 0} in stock</span>
                  </div>
                  <button type="button" onClick={() => addToCart(product)}>
                    <ShoppingCart size={17} />
                    Add to cart
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      {activePanel && <button type="button" className="panel-backdrop" aria-label="Close panel" onClick={() => setActivePanel(null)} />}

      {activePanel === 'cart' && (
        <aside className="slide-panel" aria-labelledby="cart-title">
          <div className="section-heading compact">
            <div>
              <p className="eyebrow">Checkout</p>
              <h2 id="cart-title">Cart</h2>
            </div>
            <ShoppingCart size={22} />
          </div>

          {cart.length === 0 ? (
            <div className="cart-empty">
              <p>Your cart is empty.</p>
              <span>Select products from the catalogue to begin.</span>
            </div>
          ) : (
            <div className="cart-list">
              {cart.map((item) => (
                <div className="cart-row" key={item.product.id}>
                  <div>
                    <strong>{item.product.name}</strong>
                    <span>{formatMoney(item.product.price)} each</span>
                  </div>
                  <div className="quantity-controls">
                    <button type="button" aria-label="Decrease quantity" onClick={() => updateQuantity(item.product.id, -1)}>
                      <Minus size={15} />
                    </button>
                    <span>{item.quantity}</span>
                    <button type="button" aria-label="Increase quantity" onClick={() => updateQuantity(item.product.id, 1)}>
                      <Plus size={15} />
                    </button>
                    <button type="button" aria-label="Remove item" onClick={() => removeItem(item.product.id)}>
                      <Trash2 size={15} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}

          <div className="totals">
            <div>
              <span>Subtotal</span>
              <strong>{formatMoney(cartTotal)}</strong>
            </div>
            <div>
              <span>Payment method</span>
              <strong>Card</strong>
            </div>
          </div>

          <button className="checkout-button" type="button" onClick={checkout} disabled={isCheckingOut || cart.length === 0}>
            {isCheckingOut ? <Loader2 className="spin" size={18} /> : <CreditCard size={18} />}
            Checkout and pay
          </button>

          {checkoutResult && (
            <div className="confirmation">
              <CheckCircle2 size={22} />
              <div>
                <strong>Order confirmed</strong>
                <span>Order {shortId(checkoutResult.order?.id)}</span>
                <span>Payment {shortId(checkoutResult.payment?.id)}</span>
              </div>
            </div>
          )}
        </aside>
      )}

      {activePanel === 'profile' && (
        <aside className="slide-panel profile-panel" aria-labelledby="profile-title">
          <div className="section-heading compact">
            <div>
              <p className="eyebrow">Account</p>
              <h2 id="profile-title">Profile</h2>
            </div>
            <button className="icon-button" type="button" onClick={logout} aria-label="Sign out">
              <LogOut size={20} />
            </button>
          </div>

          <div className="profile-summary">
            <div className="profile-avatar">{getInitial(auth.name || auth.email)}</div>
            <div>
              <strong>{auth.name || 'Store customer'}</strong>
              <span>{auth.email}</span>
            </div>
          </div>

          <div className="profile-details">
            <div>
              <span>Cart items</span>
              <strong>{cartItemCount}</strong>
            </div>
            <div>
              <span>Cart total</span>
              <strong>{formatMoney(cartTotal)}</strong>
            </div>
          </div>
        </aside>
      )}
    </main>
  );
}

async function apiRequest(url, options = {}, accessToken) {
  const response = await fetch(url, {
    method: options.method ?? 'GET',
    headers: {
      ...(options.body ? { 'Content-Type': 'application/json' } : {}),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  const text = await response.text();
  const data = text ? parseJson(text) : null;

  if (!response.ok) {
    throw new Error(data?.title || data?.message || text || `Request failed with ${response.status}`);
  }

  return data;
}

function parseJson(text) {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function getErrorMessage(error, fallback) {
  return error instanceof Error ? error.message : fallback;
}

function formatMoney(value) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD'
  }).format(Number(value) || 0);
}

function roundMoney(value) {
  return Math.round((Number(value) + Number.EPSILON) * 100) / 100;
}

function shortId(value) {
  return value ? String(value).slice(0, 8) : 'pending';
}

function readStoredAuth() {
  const storedAuth = localStorage.getItem(AUTH_STORAGE_KEY);

  if (!storedAuth) {
    return null;
  }

  try {
    const parsedAuth = JSON.parse(storedAuth);
    if (parsedAuth.expiresAt && new Date(parsedAuth.expiresAt) <= new Date()) {
      localStorage.removeItem(AUTH_STORAGE_KEY);
      return null;
    }

    return parsedAuth;
  } catch {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    return null;
  }
}

function getInitial(value) {
  return value ? String(value).trim().charAt(0).toUpperCase() : 'U';
}

createRoot(document.getElementById('root')).render(<App />);
