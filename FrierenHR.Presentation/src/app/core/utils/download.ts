/**
 * Saves a blob the API returned under a given file name.
 *
 * The object URL is revoked afterwards: without that, every export holds its bytes in memory
 * until the tab is closed.
 */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}
