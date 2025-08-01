import React, { useState } from "react";
import { AlertTriangle, Info, CheckCircle, XCircle } from "lucide-react";
import styles from "./UI.module.css";

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ComponentType<{ error: Error; reset: () => void }>;
}

export class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error("Error caught by boundary:", error, errorInfo);
  }

  reset = () => {
    this.setState({ hasError: false, error: undefined });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        const FallbackComponent = this.props.fallback;
        return (
          <FallbackComponent error={this.state.error!} reset={this.reset} />
        );
      }

      return (
        <div className={styles.errorBoundaryContainer}>
          <div className={`card ${styles.errorBoundaryCard}`}>
            <div className={styles.errorBoundaryContent}>
              <AlertTriangle className="w-12 h-12 text-red-500 mx-auto mb-4" />
              <h2 className={styles.errorBoundaryTitle}>
                Something went wrong
              </h2>
              <p className={styles.errorBoundaryDescription}>
                An unexpected error occurred. Please try refreshing the page.
              </p>
              <button
                onClick={this.reset}
                className={`btn-primary ${styles.errorBoundaryButton}`}
              >
                Try Again
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

// Loading Spinner Component
export const LoadingSpinner: React.FC<{ size?: "sm" | "md" | "lg" }> = ({
  size = "md",
}) => {
  const sizeClasses = {
    sm: "w-4 h-4",
    md: "w-6 h-6",
    lg: "w-8 h-8",
  };

  return (
    <div className={styles.loadingContainer}>
      <div
        className={`spinner ${styles.loadingSpinner} ${sizeClasses[size]}`}
      />
    </div>
  );
};

// Alert Component
interface AlertProps {
  type: "success" | "error" | "warning" | "info";
  title?: string;
  message: string;
  onClose?: () => void;
}

export const Alert: React.FC<AlertProps> = ({
  type,
  title,
  message,
  onClose,
}) => {
  const getIcon = () => {
    switch (type) {
      case "success":
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case "error":
        return <XCircle className="w-5 h-5 text-red-600" />;
      case "warning":
        return <AlertTriangle className="w-5 h-5 text-yellow-600" />;
      case "info":
        return <Info className="w-5 h-5 text-blue-600" />;
    }
  };

  const getClasses = () => {
    const alertClass = styles.alertBase;
    switch (type) {
      case "success":
        return `${alertClass} ${styles.alertSuccess}`;
      case "error":
        return `${alertClass} ${styles.alertError}`;
      case "warning":
        return `${alertClass} ${styles.alertWarning}`;
      case "info":
        return `${alertClass} ${styles.alertInfo}`;
    }
  };

  return (
    <div className={getClasses()}>
      {getIcon()}
      <div className={styles.alertContent}>
        {title && <h4 className={styles.alertTitle}>{title}</h4>}
        <p className={styles.alertMessage}>{message}</p>
      </div>
      {onClose && (
        <button onClick={onClose} className={styles.alertCloseButton}>
          <XCircle className="w-5 h-5" />
        </button>
      )}
    </div>
  );
};

// Empty State Component
interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
}) => {
  return (
    <div className={styles.emptyStateContainer}>
      {icon && <div className={styles.emptyStateIcon}>{icon}</div>}
      <h3 className={styles.emptyStateTitle}>{title}</h3>
      <p className={styles.emptyStateDescription}>{description}</p>
      {action && (
        <button
          onClick={action.onClick}
          className={`btn-primary ${styles.emptyStateButton}`}
        >
          {action.label}
        </button>
      )}
    </div>
  );
};

// Toast notification hook
interface Toast {
  id: string;
  type: "success" | "error" | "warning" | "info";
  title?: string;
  message: string;
  duration?: number;
}

export const useToast = () => {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const addToast = (toast: Omit<Toast, "id">) => {
    const id = Math.random().toString(36).slice(2, 11);
    const newToast = { ...toast, id };

    // Limit to 3 concurrent toasts to prevent overlap
    setToasts((prev) => {
      const newToasts = [...prev, newToast];
      return newToasts.slice(-3); // Keep only the last 3
    });

    // Auto remove after duration (reduced from 5000ms to 3000ms)
    setTimeout(() => {
      removeToast(id);
    }, toast.duration || 3000);

    return id;
  };

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id));
  };

  const ToastContainer = () => (
    <div className={styles.toastContainer}>
      {toasts.map((toast) => (
        <Alert
          key={toast.id}
          type={toast.type}
          title={toast.title}
          message={toast.message}
          onClose={() => removeToast(toast.id)}
        />
      ))}
    </div>
  );

  return { addToast, removeToast, ToastContainer };
};

// Modal Component
interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  maxWidth?: "sm" | "md" | "lg" | "xl";
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  maxWidth = "md",
}) => {
  const maxWidthClasses = {
    sm: styles.modalMaxWidthSm,
    md: styles.modalMaxWidthMd,
    lg: styles.modalMaxWidthLg,
    xl: styles.modalMaxWidthXl,
  };

  if (!isOpen) return null;

  return (
    <div className={styles.modalOverlay}>
      <div className={`${styles.modalContainer} ${maxWidthClasses[maxWidth]}`}>
        <div className={styles.modalContent}>
          <div className={styles.modalHeader}>
            <h3 className={styles.modalTitle}>{title}</h3>
            <button onClick={onClose} className={styles.modalCloseButton}>
              <XCircle className="w-5 h-5" />
            </button>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
};
