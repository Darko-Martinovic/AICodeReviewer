import { useState } from "react";
import { Alert } from "../components/UI";
import styles from "../components/UI.module.css";

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
