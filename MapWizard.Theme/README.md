# MapWizard.Theme

This project is the visual-system boundary for the desktop application. It is deliberately independent of `MapWizard.Desktop` so application workflows never leak into the theme.

## Owned here

- Semantic color, spacing, radius, and motion tokens.
- Noir and Classic palettes.
- Avalonia control styles and interaction states.
- Purely visual reusable controls such as `Surface`, `SectionGroup`, `BusyArea`, and `FieldTextBox`.
- The client-side-decorated `MapWizardWindow` shell and compositor animation helpers.

## Owned by MapWizard.Desktop

- Settings page structure and persistence.
- Modals, notifications, navigation, and update workflows.
- Feature-specific controls and view models.
- OS integration and other application services.

New visual values should be expressed as semantic `MapWizard*` resources instead of styling a feature with package-internal resource names. A reusable control belongs here only when it has no dependency on application services or feature models.
