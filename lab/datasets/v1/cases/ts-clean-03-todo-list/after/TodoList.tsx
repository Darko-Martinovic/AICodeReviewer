import { FormEvent, useState } from "react";

interface Todo {
  id: string;
  text: string;
  done: boolean;
}

export function TodoList() {
  const [todos, setTodos] = useState<Todo[]>([]);
  const [text, setText] = useState("");

  const add = (event: FormEvent) => {
    event.preventDefault();
    const trimmed = text.trim();
    if (!trimmed) return;
    setTodos((current) => [...current, { id: crypto.randomUUID(), text: trimmed, done: false }]);
    setText("");
  };

  const toggle = (id: string) =>
    setTodos((current) => current.map((t) => (t.id === id ? { ...t, done: !t.done } : t)));

  const remove = (id: string) => setTodos((current) => current.filter((t) => t.id !== id));

  return (
    <section>
      <form onSubmit={add}>
        <input value={text} onChange={(e) => setText(e.target.value)} aria-label="New to-do" />
        <button type="submit">Add</button>
      </form>
      <ul>
        {todos.map((todo) => (
          <li key={todo.id}>
            <label>
              <input type="checkbox" checked={todo.done} onChange={() => toggle(todo.id)} />
              {todo.text}
            </label>
            <button onClick={() => remove(todo.id)} aria-label={`Remove ${todo.text}`}>
              Remove
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
