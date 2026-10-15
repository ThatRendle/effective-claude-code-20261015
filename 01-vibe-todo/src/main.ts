import "./style.css";

interface Task {
  id: string;
  text: string;
  done: boolean;
}

const STORAGE_KEY = "vibe-todo.tasks";

function load(): Task[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Task[]) : [];
  } catch {
    return [];
  }
}

function save(): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(tasks));
  } catch {
    // storage unavailable; keep working in memory
  }
}

let tasks: Task[] = load();

const form = document.querySelector<HTMLFormElement>("#add-form")!;
const input = document.querySelector<HTMLInputElement>("#new-task")!;
const list = document.querySelector<HTMLUListElement>("#list")!;
const summary = document.querySelector<HTMLParagraphElement>("#summary")!;

function render(): void {
  list.replaceChildren(
    ...tasks.map((task, i) => {
      const li = document.createElement("li");
      li.className = task.done ? "done" : "";
      li.style.setProperty("--hue", String((i * 47) % 360));

      const check = document.createElement("input");
      check.type = "checkbox";
      check.checked = task.done;
      check.setAttribute("aria-label", `Mark "${task.text}" as done`);
      check.addEventListener("change", () => {
        task.done = check.checked;
        save();
        render();
      });

      const label = document.createElement("span");
      label.textContent = task.text;

      const del = document.createElement("button");
      del.type = "button";
      del.className = "delete";
      del.textContent = "✕";
      del.setAttribute("aria-label", `Delete "${task.text}"`);
      del.addEventListener("click", () => {
        tasks = tasks.filter((t) => t.id !== task.id);
        save();
        render();
      });

      li.append(check, label, del);
      return li;
    }),
  );

  const left = tasks.filter((t) => !t.done).length;
  summary.textContent = tasks.length
    ? `${left} to go · ${tasks.length - left} done 🎉`
    : "Nothing here yet. Add your first task!";
}

form.addEventListener("submit", (e) => {
  e.preventDefault();
  const text = input.value.trim();
  if (!text) return;
  tasks.push({ id: crypto.randomUUID(), text, done: false });
  input.value = "";
  save();
  render();
});

render();
