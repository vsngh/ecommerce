import React, { useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { HubConnectionBuilder } from '@microsoft/signalr';
import CustomerServiceChatbot from './CustomerServiceChatbot.jsx';
import {
  CheckCircle2,
  CreditCard,
  ClipboardList,
  Info,
  Loader2,
  LogOut,
  Minus,
  Package,
  Pencil,
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
const INVENTORY_API = '/api/inventory/api/inventory';
const PAYMENTS_API = '/api/payments/api/payments';

const starterProducts = [
  {
    name: 'Oxford Shirt',
    description: 'Clean everyday shirt with a structured collar.',
    price: 1499,
    stockQuantity: 80
  },
  {
    name: 'Canvas Tote',
    description: 'Durable carry bag for daily shopping and work.',
    price: 799,
    stockQuantity: 120
  },
  {
    name: 'Running Sneakers',
    description: 'Lightweight shoes built for comfort and movement.',
    price: 4499,
    stockQuantity: 45
  },
  {
    name: 'Desk Lamp',
    description: 'Adjustable blue-white task lamp for focused work.',
    price: 1299,
    stockQuantity: 60
  },
  {
    name: 'Cotton T-Shirt',
    description: 'Soft, breathable cotton tee for everyday wear.',
    price: 699,
    stockQuantity: 150
  },
  {
    name: 'Denim Jeans',
    description: 'Classic straight-fit jeans for casual outings.',
    price: 1999,
    stockQuantity: 75
  },
  {
    name: 'Leather Wallet',
    description: 'Compact bifold wallet with space for cards and cash.',
    price: 1199,
    stockQuantity: 55
  },
  {
    name: 'Classic Backpack',
    description: 'Padded everyday backpack with a roomy laptop sleeve.',
    price: 2499,
    stockQuantity: 60
  },
  {
    name: 'Ceramic Coffee Mug',
    description: 'Durable glazed mug for coffee, tea, and other hot drinks.',
    price: 399,
    stockQuantity: 100
  },
  {
    name: 'Wireless Headphones',
    description: 'Over-ear wireless headphones with cushioned ear cups.',
    price: 3499,
    stockQuantity: 40
  },
  {
    name: 'Smart Watch',
    description: 'Everyday smartwatch with activity and notification tracking.',
    price: 5999,
    stockQuantity: 25
  },
  {
    name: 'Steel Water Bottle',
    description: 'Reusable insulated bottle for work, travel, and the gym.',
    price: 599,
    stockQuantity: 100
  },
  {
    name: 'Yoga Mat',
    description: 'Non-slip exercise mat for yoga and home workouts.',
    price: 999,
    stockQuantity: 45
  },
  {
    name: 'Bluetooth Speaker',
    description: 'Portable speaker for music at home or on the go.',
    price: 2299,
    stockQuantity: 35
  },
  {
    name: 'Desk Organizer',
    description: 'Compact organizer for stationery and small desk essentials.',
    price: 899,
    stockQuantity: 50
  },
  {
    name: 'Travel Duffel Bag',
    description: 'Lightweight weekender bag with a spacious main compartment.',
    price: 1799,
    stockQuantity: 30
  }
];

function OrderDetails({ order, products, onCancel, isCancelling }) {
  const items = Array.isArray(order.orderItems) ? order.orderItems : [];

  return (
    <div className="order-details">
      <div className="order-detail-summary">
        <div>
          <span>Order number</span>
          <strong>{shortId(order.id)}</strong>
        </div>
        <div>
          <span>Placed</span>
          <strong>{formatDate(order.orderedAt)}</strong>
        </div>
        <div>
          <span>Status</span>
          <strong className="order-status">{order.status}</strong>
        </div>
      </div>

      <div className="order-detail-items">
        <h3>Items</h3>
        {items.length === 0 ? (
          <p className="order-empty-items">No item details are available for this order.</p>
        ) : (
          items.map((item) => {
            return (
              <article className="order-detail-item" key={item.id}>
                <div>
                  <strong>{getProductLabel(item.productId, products)}</strong>
                  <span>Quantity: {item.quantity}</span>
                </div>
                <strong>{formatMoney(item.unitPrice * item.quantity)}</strong>
              </article>
            );
          })
        )}
      </div>

      <div className="order-detail-total">
        <span>Total</span>
        <strong>{formatMoney(order.totalAmount)}</strong>
      </div>
      {order.status?.toLowerCase() === 'pending' && (
        <div className="order-cancel-action">
          <p>Refunds in this demo update the payment record only; no money is moved.</p>
          <button
            type="button"
            className="danger-button"
            onClick={() => onCancel(order)}
            disabled={isCancelling}
          >
            {isCancelling ? <Loader2 className="spin" size={17} /> : <Trash2 size={17} />}
            Cancel order
          </button>
        </div>
      )}
    </div>
  );
}

function App() {
  const [auth, setAuth] = useState(() => readStoredAuth());
  const [loginForm, setLoginForm] = useState({ name: '', email: '', password: '' });
  const [isCreatingAccount, setIsCreatingAccount] = useState(false);
  const [isLoggingIn, setIsLoggingIn] = useState(false);
  const [loginError, setLoginError] = useState('');
  const [products, setProducts] = useState([]);
  const [catalogueQuery, setCatalogueQuery] = useState('');
  const [catalogueSort, setCatalogueSort] = useState('featured');
  const [inStockOnly, setInStockOnly] = useState(false);
  const [orders, setOrders] = useState([]);
  const [orderActionMessage, setOrderActionMessage] = useState('');
  const [cancellingOrderId, setCancellingOrderId] = useState(null);
  const [adminOrders, setAdminOrders] = useState([]);
  const [inventoryItems, setInventoryItems] = useState([]);
  const [inventoryQuantityEdits, setInventoryQuantityEdits] = useState({});
  const [selectedOrderId, setSelectedOrderId] = useState(null);
  const [selectedProductId, setSelectedProductId] = useState(null);
  const [editingProductId, setEditingProductId] = useState(null);
  const [deletingProductId, setDeletingProductId] = useState(null);
  const [productForm, setProductForm] = useState({
    name: '',
    description: '',
    price: '',
    stockQuantity: ''
  });
  const [cart, setCart] = useState([]);
  const [isLoadingProducts, setIsLoadingProducts] = useState(false);
  const [isLoadingOrders, setIsLoadingOrders] = useState(false);
  const [ordersError, setOrdersError] = useState('');
  const [isLoadingAdminData, setIsLoadingAdminData] = useState(false);
  const [adminDataError, setAdminDataError] = useState('');
  const [savingInventoryId, setSavingInventoryId] = useState(null);
  const [isSeeding, setIsSeeding] = useState(false);
  const [isSavingProduct, setIsSavingProduct] = useState(false);
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [notice, setNotice] = useState('');
  const [checkoutResult, setCheckoutResult] = useState(null);
  const [activePanel, setActivePanel] = useState(null);

  useEffect(() => {
    if (auth) {
      loadProducts();
      if (auth.role?.toLowerCase() === 'admin') {
        loadAdminData();
      }
    }
  }, [auth]);

  useEffect(() => {
    if (!auth?.accessToken) {
      return undefined;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${ORDERS_API}/hubs/notifications`, {
        accessTokenFactory: () => auth.accessToken
      })
      .withAutomaticReconnect()
      .build();
    let reconnectTimer;
    let isDisposed = false;

    const scheduleReconnect = () => {
      if (isDisposed || reconnectTimer) {
        return;
      }

      reconnectTimer = window.setTimeout(() => {
        reconnectTimer = undefined;
        void startConnection();
      }, 5000);
    };

    const startConnection = async () => {
      try {
        await connection.start();
      } catch {
        scheduleReconnect();
      }
    };

    const onOrderStatusChanged = (notification) => {
      if (!notification?.orderId || !notification.status) {
        return;
      }

      setOrders((currentOrders) => currentOrders.map((order) =>
        order.id === notification.orderId
          ? { ...order, status: notification.status }
          : order
      ));
      setOrderActionMessage(`Order ${shortId(notification.orderId)} status changed to ${notification.status}.`);
    };

    connection.on('OrderStatusChanged', onOrderStatusChanged);
    connection.onclose(scheduleReconnect);
    void startConnection();

    return () => {
      isDisposed = true;
      if (reconnectTimer) {
        window.clearTimeout(reconnectTimer);
      }
      connection.off('OrderStatusChanged', onOrderStatusChanged);
      void connection.stop();
    };
  }, [auth?.accessToken]);

  const cartTotal = useMemo(
    () => cart.reduce((total, item) => total + item.product.price * item.quantity, 0),
    [cart]
  );

  const cartItemCount = useMemo(
    () => cart.reduce((total, item) => total + item.quantity, 0),
    [cart]
  );

  const filteredProducts = useMemo(() => {
    const query = catalogueQuery.trim().toLocaleLowerCase();
    const matchingProducts = products.filter((product) => {
      const matchesQuery = !query ||
        `${product.name ?? ''} ${product.description ?? ''}`.toLocaleLowerCase().includes(query);
      const matchesStock = !inStockOnly || Number(product.stockQuantity ?? 0) > 0;
      return matchesQuery && matchesStock;
    });

    if (catalogueSort === 'name') {
      return matchingProducts.sort((left, right) => left.name.localeCompare(right.name));
    }

    if (catalogueSort === 'price-ascending') {
      return matchingProducts.sort((left, right) => Number(left.price) - Number(right.price));
    }

    if (catalogueSort === 'price-descending') {
      return matchingProducts.sort((left, right) => Number(right.price) - Number(left.price));
    }

    return matchingProducts;
  }, [products, catalogueQuery, catalogueSort, inStockOnly]);

  const isAdmin = auth?.role?.toLowerCase() === 'admin';
  const selectedOrder = orders.find((order) => order.id === selectedOrderId);
  const selectedProduct = products.find((product) => product.id === selectedProductId);

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

  async function loadOrders() {
    if (!auth) {
      return;
    }

    setIsLoadingOrders(true);
    setOrdersError('');

    try {
      const records = await apiRequest(`${ORDERS_API}/mine`, {}, auth.accessToken);
      if (!Array.isArray(records)) {
        throw new Error('Order history response was invalid.');
      }

      setOrders(records);
    } catch (error) {
      setOrders([]);
      setOrdersError(getErrorMessage(error, 'Unable to load order history.'));
    } finally {
      setIsLoadingOrders(false);
    }
  }

  async function getOrdersForAssistant() {
    const records = await apiRequest(`${ORDERS_API}/mine`, {}, auth.accessToken);
    if (!Array.isArray(records)) {
      throw new Error('Order history response was invalid.');
    }
    return records;
  }

  async function loadAdminData() {
    if (!auth) {
      return;
    }

    setIsLoadingAdminData(true);
    setAdminDataError('');

    try {
      const [orderRecords, inventoryRecords] = await Promise.all([
        apiRequest(ORDERS_API, {}, auth.accessToken),
        apiRequest(INVENTORY_API, {}, auth.accessToken)
      ]);
      if (!Array.isArray(orderRecords) || !Array.isArray(inventoryRecords)) {
        throw new Error('Admin data response was invalid.');
      }

      setAdminOrders(orderRecords);
      setInventoryItems(inventoryRecords);
      setInventoryQuantityEdits({});
    } catch (error) {
      setAdminOrders([]);
      setInventoryItems([]);
      setAdminDataError(getErrorMessage(error, 'Unable to load admin data.'));
    } finally {
      setIsLoadingAdminData(false);
    }
  }

  async function saveInventoryQuantity(item) {
    const rawQuantity = inventoryQuantityEdits[item.id] ?? item.quantityAvailable;
    const quantityAvailable = Number(rawQuantity);
    if (String(rawQuantity).trim() === '' || !Number.isInteger(quantityAvailable) || quantityAvailable < 0) {
      setAdminDataError('Available quantity must be a non-negative whole number.');
      return;
    }

    setSavingInventoryId(item.id);
    setAdminDataError('');

    try {
      await apiRequest(`${INVENTORY_API}/${item.id}/quantity`, {
        method: 'PATCH',
        body: { quantityAvailable }
      }, auth.accessToken);
      setInventoryItems((items) => items.map((currentItem) =>
        currentItem.id === item.id
          ? { ...currentItem, quantityAvailable, updatedAt: new Date().toISOString() }
          : currentItem
      ));
      setInventoryQuantityEdits((edits) => {
        const updatedEdits = { ...edits };
        delete updatedEdits[item.id];
        return updatedEdits;
      });
    } catch (error) {
      setAdminDataError(getErrorMessage(error, 'Unable to update inventory quantity.'));
    } finally {
      setSavingInventoryId(null);
    }
  }

  function refreshAdminDashboard() {
    void loadProducts();
    void loadAdminData();
  }

  function openOrderHistory() {
    setActivePanel('orders');
    setSelectedOrderId(null);
    setOrderActionMessage('');
    void loadOrders();
  }

  async function cancelOrder(order) {
    if (!window.confirm(
      `Cancel order ${shortId(order.id)}? Any completed payment will be marked Refunded in this demo, but no funds will be moved.`
    )) {
      return;
    }

    setCancellingOrderId(order.id);
    setOrderActionMessage('');
    try {
      await apiRequest(`${ORDERS_API}/${order.id}/cancel`, {
        method: 'PATCH'
      }, auth.accessToken);
      setOrders((currentOrders) => currentOrders.map((currentOrder) =>
        currentOrder.id === order.id ? { ...currentOrder, status: 'Cancelled' } : currentOrder
      ));
      setOrderActionMessage('Order cancelled. Any completed payment is marked Refunded in the demo; no money was moved.');
    } catch (error) {
      setOrderActionMessage(getErrorMessage(error, 'Unable to cancel this order. The order has not been changed.'));
    } finally {
      setCancellingOrderId(null);
    }
  }

  function openProductDetails(productId) {
    setSelectedProductId(productId);
    setActivePanel('product');
  }

  async function seedCatalogue() {
    setIsSeeding(true);
    setNotice('');

    try {
      const currentProducts = await apiRequest(PRODUCT_API, {}, auth.accessToken);
      if (!Array.isArray(currentProducts)) {
        throw new Error('The product catalogue response was invalid.');
      }

      const productsByName = new Map(
        currentProducts.map((product) => [product.name.trim().toLocaleLowerCase(), product])
      );
      await Promise.all(starterProducts.map(async (product) => {
        const existingProduct = productsByName.get(product.name.toLocaleLowerCase());
        if (!existingProduct) {
          await apiRequest(PRODUCT_API, {
            method: 'POST',
            body: product
          }, auth.accessToken);
        } else if (Number(existingProduct.price) !== product.price) {
          await apiRequest(`${PRODUCT_API}/${existingProduct.id}/price`, {
            method: 'PATCH',
            body: { price: product.price }
          }, auth.accessToken);
        }
      }));
      const updatedProducts = await apiRequest(PRODUCT_API, {}, auth.accessToken);
      if (!Array.isArray(updatedProducts)) {
        throw new Error('The updated product catalogue response was invalid.');
      }

      setProducts(updatedProducts);
      setNotice('Starter products are up to date. Prices are in INR.');
    } catch (error) {
      setNotice(getErrorMessage(error, 'Unable to update the starter catalogue.'));
    } finally {
      setIsSeeding(false);
    }
  }

  function editProduct(product) {
    setEditingProductId(product.id);
    setProductForm({
      name: product.name ?? '',
      description: product.description ?? '',
      price: String(product.price ?? ''),
      stockQuantity: String(product.stockQuantity ?? 0)
    });
    setNotice('');
  }

  function cancelProductEdit() {
    setEditingProductId(null);
    setProductForm({ name: '', description: '', price: '', stockQuantity: '' });
    setNotice('');
  }

  async function saveProduct(event) {
    event.preventDefault();
    const price = Number(productForm.price);
    const stockQuantity = Number(productForm.stockQuantity);
    const existingProduct = editingProductId
      ? products.find((product) => product.id === editingProductId)
      : null;

    if (!productForm.name.trim() || !productForm.description.trim() ||
      !Number.isFinite(price) || price < 0 ||
      (!editingProductId && (!Number.isInteger(stockQuantity) || stockQuantity < 0))) {
      setNotice('Enter a product name, description, non-negative price, and valid stock quantity.');
      return;
    }

    if (editingProductId && !existingProduct) {
      setNotice('The product being edited is no longer available. Refresh the catalogue and try again.');
      cancelProductEdit();
      return;
    }

    setIsSavingProduct(true);
    setNotice('');

    try {
      const requestBody = {
        name: productForm.name.trim(),
        description: productForm.description.trim(),
        price,
        stockQuantity: editingProductId
          ? Number(existingProduct.stockQuantity ?? 0)
          : stockQuantity
      };

      if (editingProductId) {
        await apiRequest(`${PRODUCT_API}/${editingProductId}`, {
          method: 'PUT',
          body: requestBody
        }, auth.accessToken);
        setProducts((currentProducts) => currentProducts.map((product) =>
          product.id === editingProductId ? { ...product, ...requestBody } : product
        ));
        setNotice('Product details were updated.');
      } else {
        const createdProduct = await apiRequest(PRODUCT_API, {
          method: 'POST',
          body: requestBody
        }, auth.accessToken);
        if (!createdProduct?.id) {
          throw new Error('The product was created, but the response did not include product details. Refresh the catalogue.');
        }

        setProducts((currentProducts) => [...currentProducts, createdProduct]);
        setNotice('Product was added to the catalogue.');
      }

      setEditingProductId(null);
      setProductForm({ name: '', description: '', price: '', stockQuantity: '' });
    } catch (error) {
      setNotice(getErrorMessage(error, editingProductId ? 'Unable to update product.' : 'Unable to add product.'));
    } finally {
      setIsSavingProduct(false);
    }
  }

  async function deleteProduct(product) {
    if (!window.confirm(`Delete "${product.name}" from the catalogue? This cannot be undone.`)) {
      return;
    }

    setDeletingProductId(product.id);
    setNotice('');

    try {
      await apiRequest(`${PRODUCT_API}/${product.id}`, {
        method: 'DELETE'
      }, auth.accessToken);
      setProducts((currentProducts) => currentProducts.filter((currentProduct) => currentProduct.id !== product.id));
      setCart((items) => items.filter((item) => item.product.id !== product.id));
      if (editingProductId === product.id) {
        cancelProductEdit();
      }
      if (selectedProductId === product.id) {
        setSelectedProductId(null);
      }
      setNotice('Product was removed from the catalogue.');
    } catch (error) {
      setNotice(getErrorMessage(error, 'Unable to delete product.'));
    } finally {
      setDeletingProductId(null);
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
    setOrders([]);
    setAdminOrders([]);
    setInventoryItems([]);
    setInventoryQuantityEdits({});
    setSelectedOrderId(null);
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
          currency: 'INR',
          paymentMethod: 'Card',
          status: 'Pending'
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
              <p>Orders, inventory, and catalogue</p>
            </div>
          </div>

          <nav className="header-actions" aria-label="Admin actions">
            <button type="button" className="nav-button" onClick={refreshAdminDashboard} disabled={isLoadingProducts || isLoadingAdminData}>
              {isLoadingProducts || isLoadingAdminData ? <Loader2 className="spin" size={18} /> : <RefreshCw size={18} />}
              <span>Refresh</span>
            </button>
            <button className="icon-button" type="button" onClick={logout} aria-label="Sign out">
              <LogOut size={20} />
            </button>
          </nav>
        </header>

        {notice && <p className="notice">{notice}</p>}
        {adminDataError && <p className="admin-data-error" role="alert">{adminDataError}</p>}

        <section className="admin-grid">
          <form className="admin-panel" onSubmit={saveProduct} aria-labelledby="add-product-title">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">Products</p>
                <h2 id="add-product-title">{editingProductId ? 'Edit Product' : 'Add Product'}</h2>
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
                  <span>MRP (INR)</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={productForm.price}
                    onChange={(event) => setProductForm((form) => ({ ...form, price: event.target.value }))}
                    required
                  />
                </label>
                {!editingProductId && (
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
                )}
              </div>
              {editingProductId && <p className="form-hint">Stock is managed in the Inventory section and will not be changed here.</p>}
            </div>

            <div className="product-form-actions">
              <button className="login-button" type="submit" disabled={isSavingProduct}>
                {isSavingProduct ? <Loader2 className="spin" size={18} /> : <Save size={18} />}
                {editingProductId ? 'Save changes' : 'Add product'}
              </button>
              {editingProductId && (
                <button className="secondary-button" type="button" onClick={cancelProductEdit} disabled={isSavingProduct}>
                  Cancel
                </button>
              )}
            </div>
          </form>

          <section className="admin-panel" aria-labelledby="admin-catalogue-title">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">Catalogue</p>
                <h2 id="admin-catalogue-title">Current Products</h2>
              </div>
              <div className="admin-catalogue-actions">
                <span className="catalogue-count">{filteredProducts.length} of {products.length} products</span>
                <button
                  type="button"
                  className="secondary-button"
                  onClick={seedCatalogue}
                  disabled={isSeeding || isLoadingProducts}
                >
                  {isSeeding ? <Loader2 className="spin" size={17} /> : <Plus size={17} />}
                  {isSeeding ? 'Updating catalogue' : 'Add starter products'}
                </button>
              </div>
            </div>

            {products.length > 0 && (
              <div className="product-filters" aria-label="Product filters">
                <label className="product-search">
                  <span>Search products</span>
                  <input
                    type="search"
                    value={catalogueQuery}
                    onChange={(event) => setCatalogueQuery(event.target.value)}
                    placeholder="Search by name or description"
                  />
                </label>
                <label className="product-sort">
                  <span>Sort by</span>
                  <select value={catalogueSort} onChange={(event) => setCatalogueSort(event.target.value)}>
                    <option value="featured">Featured</option>
                    <option value="name">Name: A to Z</option>
                    <option value="price-ascending">Price: Low to high</option>
                    <option value="price-descending">Price: High to low</option>
                  </select>
                </label>
                <label className="stock-filter">
                  <input
                    type="checkbox"
                    checked={inStockOnly}
                    onChange={(event) => setInStockOnly(event.target.checked)}
                  />
                  <span>In stock only</span>
                </label>
              </div>
            )}

            {isLoadingProducts ? (
              <div className="empty-state compact-empty">
                <Loader2 className="spin" size={24} />
                <p>Loading products</p>
              </div>
            ) : products.length === 0 ? (
              <div className="empty-state compact-empty">
                <Package size={28} />
                <h3>No products found</h3>
                <p>Add starter products or create a product manually.</p>
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
                    <div className="admin-product-actions">
                      <button
                        type="button"
                        onClick={() => editProduct(product)}
                        aria-label={`Edit ${product.name}`}
                      >
                        <Pencil size={15} />
                        Edit
                      </button>
                      <button
                        className="danger-button"
                        type="button"
                        onClick={() => deleteProduct(product)}
                        disabled={deletingProductId === product.id}
                        aria-label={`Delete ${product.name}`}
                      >
                        {deletingProductId === product.id
                          ? <Loader2 className="spin" size={15} />
                          : <Trash2 size={15} />}
                        Delete
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>

          <section className="admin-panel admin-wide" aria-labelledby="admin-orders-title">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">Orders</p>
                <h2 id="admin-orders-title">Recent Orders</h2>
              </div>
              <span className="catalogue-count">{adminOrders.length} orders</span>
            </div>

            {isLoadingAdminData ? (
              <div className="empty-state compact-empty">
                <Loader2 className="spin" size={24} />
                <p>Loading orders</p>
              </div>
            ) : adminOrders.length === 0 ? (
              <div className="cart-empty">
                <p>No orders found.</p>
                <span>Orders will appear here after customers check out.</span>
              </div>
            ) : (
              <div className="admin-record-list">
                {adminOrders.map((order) => {
                  const items = Array.isArray(order.orderItems) ? order.orderItems : [];

                  return (
                    <article className="admin-order-row" key={order.id}>
                      <div className="admin-order-summary">
                        <div>
                          <strong>Order {shortId(order.id)}</strong>
                          <span>{formatDate(order.orderedAt)} · Customer {shortId(order.userId)}</span>
                        </div>
                        <div className="admin-order-meta">
                          <span className="order-status">{order.status}</span>
                          <strong>{formatMoney(order.totalAmount)}</strong>
                        </div>
                      </div>
                      <details className="admin-order-details">
                        <summary>View {items.length} {items.length === 1 ? 'item' : 'items'}</summary>
                        {items.length === 0 ? (
                          <p className="order-empty-items">No item details are available for this order.</p>
                        ) : (
                          <ul>
                            {items.map((item) => (
                              <li key={item.id}>
                                {getProductLabel(item.productId, products)} · {item.quantity} × {formatMoney(item.unitPrice)}
                              </li>
                            ))}
                          </ul>
                        )}
                      </details>
                    </article>
                  );
                })}
              </div>
            )}
          </section>

          <section className="admin-panel admin-wide" aria-labelledby="admin-inventory-title">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">Stock</p>
                <h2 id="admin-inventory-title">Inventory</h2>
              </div>
              <span className="catalogue-count">{inventoryItems.length} items</span>
            </div>

            {isLoadingAdminData ? (
              <div className="empty-state compact-empty">
                <Loader2 className="spin" size={24} />
                <p>Loading inventory</p>
              </div>
            ) : inventoryItems.length === 0 ? (
              <div className="cart-empty">
                <p>No inventory records found.</p>
                <span>Create inventory records before updating stock quantities.</span>
              </div>
            ) : (
              <div className="admin-record-list">
                {inventoryItems.map((item) => {
                  const quantity = inventoryQuantityEdits[item.id] ?? item.quantityAvailable;
                  const isUnchanged = String(quantity) === String(item.quantityAvailable);

                  return (
                    <article className="admin-stock-row" key={item.id}>
                      <div className="admin-stock-product">
                        <strong>{getProductLabel(item.productId, products)}</strong>
                        <span>{item.quantityReserved} reserved · Updated {formatDate(item.updatedAt)}</span>
                      </div>
                      <label className="admin-stock-input">
                        <span>Available</span>
                        <input
                          type="number"
                          min="0"
                          step="1"
                          value={quantity}
                          onChange={(event) => setInventoryQuantityEdits((edits) => ({
                            ...edits,
                            [item.id]: event.target.value
                          }))}
                          aria-label={`Available quantity for ${getProductLabel(item.productId, products)}`}
                          required
                        />
                      </label>
                      <button
                        type="button"
                        onClick={() => saveInventoryQuantity(item)}
                        disabled={isUnchanged || savingInventoryId === item.id}
                      >
                        {savingInventoryId === item.id ? <Loader2 className="spin" size={17} /> : <Save size={17} />}
                        Save
                      </button>
                    </article>
                  );
                })}
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
          <button type="button" className="icon-button" onClick={openOrderHistory} aria-label="View order history">
            <ClipboardList size={20} />
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
            <p className="eyebrow">Shop · MRP in INR</p>
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
            <p>The catalogue is empty right now. Please check back later.</p>
          </div>
        ) : filteredProducts.length === 0 ? (
          <div className="empty-state">
            <Package size={28} />
            <h3>No matching products</h3>
            <p>Try a different search or clear the active filters.</p>
            <button
              type="button"
              onClick={() => {
                setCatalogueQuery('');
                setCatalogueSort('featured');
                setInStockOnly(false);
              }}
            >
              Clear filters
            </button>
          </div>
        ) : (
          <div className="product-grid">
            {filteredProducts.map((product) => (
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
                    <span>MRP · {product.stockQuantity ?? 0} in stock</span>
                  </div>
                  <div className="product-actions">
                    <button className="product-details-button" type="button" onClick={() => openProductDetails(product.id)}>
                      <Info size={17} />
                      View details
                    </button>
                    <button
                      type="button"
                      onClick={() => addToCart(product)}
                      disabled={Number(product.stockQuantity ?? 0) <= 0}
                    >
                      <ShoppingCart size={17} />
                      {Number(product.stockQuantity ?? 0) > 0 ? 'Add to cart' : 'Out of stock'}
                    </button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      {activePanel && <button type="button" className="panel-backdrop" aria-label="Close panel" onClick={() => setActivePanel(null)} />}

      {activePanel === 'product' && (
        <aside className="slide-panel product-detail-panel" aria-labelledby="product-details-title">
          <div className="section-heading compact">
            <div>
              <p className="eyebrow">Product</p>
              <h2 id="product-details-title">Details</h2>
            </div>
            <Package size={22} />
          </div>

          {selectedProduct ? (
            <div className="product-detail-content">
              <div className="product-detail-image">
                <Package size={42} />
              </div>
              <div className="product-detail-copy">
                <h3>{selectedProduct.name}</h3>
                <p>{selectedProduct.description || 'No description available.'}</p>
              </div>
              <div className="product-detail-meta">
                <strong>{formatMoney(selectedProduct.price)}</strong>
                <span>
                  MRP ·{' '}
                  {Number(selectedProduct.stockQuantity ?? 0) > 0
                    ? `${selectedProduct.stockQuantity} in stock`
                    : 'Out of stock'}
                </span>
              </div>
              <button
                className="checkout-button"
                type="button"
                onClick={() => addToCart(selectedProduct)}
                disabled={Number(selectedProduct.stockQuantity ?? 0) <= 0}
              >
                <ShoppingCart size={18} />
                Add to cart
              </button>
              <button type="button" className="link-button" onClick={() => setActivePanel(null)}>
                Back to catalogue
              </button>
            </div>
          ) : (
            <div className="cart-empty">
              <p>Product unavailable</p>
              <span>The catalogue may have changed. Refresh products to see the latest items.</span>
              <button type="button" onClick={loadProducts}>Refresh catalogue</button>
            </div>
          )}
        </aside>
      )}

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
          <button type="button" className="link-button" onClick={openOrderHistory}>
            View order history
          </button>
        </aside>
      )}

      {activePanel === 'orders' && (
        <aside className="slide-panel orders-panel" aria-labelledby="orders-title">
          <div className="section-heading compact">
            <div>
              <p className="eyebrow">Purchases</p>
              <h2 id="orders-title">{selectedOrder ? 'Order details' : 'My orders'}</h2>
            </div>
            {selectedOrder ? (
              <button type="button" className="link-button" onClick={() => setSelectedOrderId(null)}>
                All orders
              </button>
            ) : (
              <ClipboardList size={22} />
            )}
          </div>

          {orderActionMessage && <p className="order-action-message" role="status">{orderActionMessage}</p>}

          {isLoadingOrders ? (
            <div className="cart-empty">
              <Loader2 className="spin" size={22} />
              <p>Loading order history</p>
            </div>
          ) : ordersError ? (
            <div className="cart-empty order-error">
              <p>Unable to load orders</p>
              <span>{ordersError}</span>
              <button type="button" onClick={loadOrders}>Try again</button>
            </div>
          ) : selectedOrder ? (
            <>
              <OrderDetails
                order={selectedOrder}
                products={products}
                onCancel={cancelOrder}
                isCancelling={cancellingOrderId === selectedOrder.id}
              />
            </>
          ) : orders.length === 0 ? (
            <div className="cart-empty">
              <p>No orders yet.</p>
              <span>Your completed purchases will appear here.</span>
            </div>
          ) : (
            <div className="order-list">
              {orders.map((order) => (
                <button
                  type="button"
                  className="order-row"
                  key={order.id}
                  onClick={() => setSelectedOrderId(order.id)}
                >
                  <span className="order-row-main">
                    <strong>Order {shortId(order.id)}</strong>
                    <span>{formatDate(order.orderedAt)}</span>
                  </span>
                  <span className="order-row-meta">
                    <span className="order-status">{order.status}</span>
                    <strong>{formatMoney(order.totalAmount)}</strong>
                  </span>
                </button>
              ))}
            </div>
          )}
        </aside>
      )}

      {!isAdmin && (
        <CustomerServiceChatbot products={products} getOrders={getOrdersForAssistant} />
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
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR'
  }).format(Number(value) || 0);
}

function formatDate(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Date unavailable';
  }

  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(date);
}

function getProductLabel(productId, products) {
  const product = products.find((record) => record.id === productId);
  return product?.name || `Product ${shortId(productId)}`;
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
