# Flujo de trabajo

## Ramas

```
master          ← producción estable
  └── develop   ← integración (rama por defecto)
        └── feature/N-nombre
        └── bugfix/N-nombre
```

- **master**: solo recibe merges desde `develop` al cerrar un milestone (v0.1, v0.2, v0.3)
- **develop**: base de todo el trabajo, protegida — no se pushea directo
- **feature/N-nombre**: una rama por issue, siempre desde `develop`

## Cómo trabajar un issue

```bash
# 1. Asegurarse de tener develop actualizado
git checkout develop
git pull

# 2. Crear la rama del issue
git checkout -b feature/5-tobogan

# 3. Trabajar... commitear...
git add .
git commit -m "feat: agregar tobogan con primitivas (#5)"

# 4. Push y abrir PR hacia develop
git push -u origin feature/5-tobogan
gh pr create --base develop --title "feat: tobogan" --body "Closes #5"
```

## Convención de commits

| Prefijo | Cuándo usarlo |
|---------|---------------|
| `feat:` | Nueva funcionalidad |
| `art:` | Assets, materiales, texturas |
| `fix:` | Corrección de bug |
| `scene:` | Cambios de escena o configuración Unity |
| `chore:` | Mantenimiento, estructura, sin cambio funcional |

Incluir `(#N)` o `Closes #N` en el mensaje para linkear el commit al issue.

## Cerrar un milestone

Cuando todas las tareas de un milestone están en `develop`:

```bash
git checkout master
git merge develop
git tag v0.1
git push origin master --tags
```
