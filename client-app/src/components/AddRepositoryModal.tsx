import React from "react";
import styles from "./AddRepositoryModal.module.css";

interface NewRepoForm {
  owner: string;
  name: string;
  show: boolean;
}

interface AddRepositoryModalProps {
  newRepoForm: NewRepoForm;
  onFormChange: (updates: Partial<NewRepoForm>) => void;
  onAddRepository: () => void;
  onClose: () => void;
}

export const AddRepositoryModal: React.FC<AddRepositoryModalProps> = ({
  newRepoForm,
  onFormChange,
  onAddRepository,
  onClose,
}) => {
  if (!newRepoForm.show) {
    return null;
  }

  const handleOwnerChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFormChange({ owner: e.target.value });
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFormChange({ name: e.target.value });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onAddRepository();
  };

  const handleCancel = () => {
    onClose();
  };

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div className={styles.modalOverlay} onClick={handleOverlayClick}>
      <div className={styles.modalContainer}>
        <form onSubmit={handleSubmit}>
          <div className={styles.modalContent}>
            <h3 className={styles.modalTitle}>Add Repository</h3>

            <div className={styles.formContainer}>
              <div>
                <label className={styles.fieldLabel}>Owner</label>
                <input
                  type="text"
                  value={newRepoForm.owner}
                  onChange={handleOwnerChange}
                  className={styles.fieldInput}
                  placeholder="e.g., microsoft"
                  required
                  autoFocus
                />
              </div>

              <div>
                <label className={styles.fieldLabel}>Repository Name</label>
                <input
                  type="text"
                  value={newRepoForm.name}
                  onChange={handleNameChange}
                  className={styles.fieldInput}
                  placeholder="e.g., vscode"
                  required
                />
              </div>
            </div>

            <div className={styles.buttonContainer}>
              <button
                type="button"
                onClick={handleCancel}
                className={`${styles.button} ${styles.buttonSecondary}`}
              >
                Cancel
              </button>
              <button
                type="submit"
                className={`${styles.button} ${styles.buttonPrimary}`}
              >
                Add Repository
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AddRepositoryModal;
