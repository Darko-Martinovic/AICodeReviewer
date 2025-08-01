import { useState } from "react";

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
      return newToasts.slice(-3); // Keep only last 3 toasts
    });

    // Auto-remove after duration
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, newToast.duration || 5000);
  };

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  const ToastContainer = () => (
    <div className="fixed top-4 right-4 z-[9999] space-y-2">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`
            transform transition-all duration-300 ease-in-out
            max-w-sm w-full shadow-lg rounded-lg p-4
            ${
              toast.type === "success"
                ? "bg-green-50 border border-green-200 text-green-800"
                : toast.type === "error"
                ? "bg-red-50 border border-red-200 text-red-800"
                : toast.type === "warning"
                ? "bg-yellow-50 border border-yellow-200 text-yellow-800"
                : "bg-blue-50 border border-blue-200 text-blue-800"
            }
          `}
          onClick={() => removeToast(toast.id)}
        >
          <div className="flex">
            <div className="flex-1">
              {toast.title && (
                <p className="text-sm font-medium">{toast.title}</p>
              )}
              <p className={`text-sm ${toast.title ? "mt-1" : ""}`}>
                {toast.message}
              </p>
            </div>
            <button
              onClick={(e) => {
                e.stopPropagation();
                removeToast(toast.id);
              }}
              className="ml-3 flex-shrink-0 text-gray-400 hover:text-gray-600"
            >
              ×
            </button>
          </div>
        </div>
      ))}
    </div>
  );

  return { addToast, removeToast, ToastContainer };
};

export type { Toast };
