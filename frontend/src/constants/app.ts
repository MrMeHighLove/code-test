export const ROUTES = {
  login: '/login',
  register: '/register',
  products: '/products',
} as const;

export const PRODUCT_PAGE = {
  pageSize: 5,
  allColoursValue: '',
  labels: {
    title: 'Products',
    logout: 'Logout',
    addProduct: 'Add Product',
    add: 'Add',
    allProducts: 'All Products',
    filterByColour: 'Filter by colour:',
    previous: 'Previous',
    next: 'Next',
    noProductsFound: 'No products found.',
    pageSummary: (page: number, totalPages: number) => `Page ${page} of ${totalPages}`,
    resultsSummary: (totalItems: number) => `${totalItems} product${totalItems === 1 ? '' : 's'}`,
  },
  placeholders: {
    name: 'Name',
    description: 'Description (optional)',
    price: 'Price',
  },
  errors: {
    createFailed: 'Failed to create product.',
  },
} as const;

export const AUTH_PAGE = {
  loginTitle: 'Login',
  registerTitle: 'Register',
  usernamePlaceholder: 'Username',
  registerUsernamePlaceholder: 'Username (min 3 chars)',
  passwordPlaceholder: 'Password',
  registerPasswordPlaceholder: 'Password (min 6 chars)',
  loginButton: 'Login',
  registerButton: 'Register',
  invalidCredentials: 'Invalid credentials. Please try again.',
  registrationFailed: 'Registration failed.',
  registrationSuccess: 'Registration successful! Redirecting to login...',
  loginPrompt: "Don't have an account?",
  registerPrompt: 'Already have an account?',
} as const;
