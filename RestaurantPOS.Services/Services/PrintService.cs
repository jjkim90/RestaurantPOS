using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RestaurantPOS.Services
{
    public class PrintService : IPrintService
    {
        private const string STORE_NAME = "맛있는 식당";
        private const string STORE_PHONE = "Tel: 02-1234-5678";
        private const string SEPARATOR_LINE = "=====================================";
        private const string DASH_LINE = "-------------------------------------";

        public async Task<bool> PrintReceiptAsync(OrderDTO order)
        {
            return await PrintReceiptInternalAsync(order, false);
        }

        public async Task<bool> ReprintReceiptAsync(OrderDTO order)
        {
            return await PrintReceiptInternalAsync(order, true);
        }

        public async Task<bool> PrintReceiptForPaymentHistoryAsync(PaymentHistoryDTO paymentHistory)
        {
            // PaymentHistoryDTO를 OrderDTO로 변환
            var order = new OrderDTO
            {
                OrderId = paymentHistory.OrderId,
                OrderNumber = paymentHistory.OrderNumber,
                TableId = paymentHistory.TableId,
                TableName = paymentHistory.TableName,
                OrderDate = paymentHistory.OrderDate,
                TotalAmount = paymentHistory.TotalAmount,
                Status = paymentHistory.OrderStatus,
                PaymentDate = paymentHistory.OrderDate,
                PaymentMethod = paymentHistory.PaymentTransactions?.FirstOrDefault()?.PaymentMethod ?? "Cash",
                OrderDetails = paymentHistory.OrderDetails,
                // 복합 결제 정보를 위해 PaymentTransactions도 전달
                PaymentTransactions = paymentHistory.PaymentTransactions
            };

            return await PrintReceiptInternalAsync(order, true, paymentHistory.PaymentTransactions);
        }

        private async Task<bool> PrintReceiptInternalAsync(OrderDTO order, bool isReprint, List<PaymentTransactionDTO> paymentTransactions = null)
        {
            try
            {
                // UI 스레드에서 실행되어야 함
                var tcs = new TaskCompletionSource<bool>();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var document = CreateReceiptDocument(order, isReprint, paymentTransactions);
                        var printDialog = new PrintDialog();

                        // 영수증 용지 크기 설정 (80mm x 297mm)
                        printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                            80 * 96 / 25.4,  // 80mm를 인치로 변환 후 DPI(96) 적용
                            297 * 96 / 25.4); // 297mm를 인치로 변환 후 DPI(96) 적용

                        // 다이얼로그 없이 기본 프린터로 바로 출력
                        printDialog.PrintDocument(
                            ((IDocumentPaginatorSource)document).DocumentPaginator,
                            $"영수증 - {order.OrderNumber}{(isReprint ? " (재출력)" : "")}");
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"영수증 출력 오류: {ex.Message}");
                        tcs.SetException(ex);
                    }
                });
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"영수증 출력 오류: {ex.Message}");
                return false;
            }
        }

        private FlowDocument CreateReceiptDocument(OrderDTO order, bool isReprint = false, List<PaymentTransactionDTO> paymentTransactions = null)
        {
            var document = new FlowDocument
            {
                PageWidth = 280,  // 80mm 영수증 너비에 맞춤
                PageHeight = 600, // 적절한 높이 설정
                PagePadding = new Thickness(10, 10, 10, 10), // 상하좌우 여백 축소
                ColumnWidth = 260, // 열 너비 제한
                FontFamily = new FontFamily("Consolas, 맑은 고딕"),
                FontSize = 10      // 폰트 크기 약간 축소
            };

            AddHeader(document, isReprint);
            AddOrderInfo(document, order, isReprint);
            AddOrderDetails(document, order);
            AddTotalAmount(document, order);
            
            if (paymentTransactions != null && paymentTransactions.Any())
            {
                AddMultiPaymentInfo(document, paymentTransactions);
            }
            else
            {
                AddPaymentInfo(document, order);
            }
            
            AddFooter(document, isReprint);

            return document;
        }

        private void AddHeader(FlowDocument document, bool isReprint = false)
        {
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            header.Inlines.Add(new Run(SEPARATOR_LINE + "\n"));
            
            if (isReprint)
            {
                header.Inlines.Add(new Run("[ 재출력 ]\n") { FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
            }
            
            header.Inlines.Add(new Run(STORE_NAME) { FontSize = 16, FontWeight = FontWeights.Bold });
            header.Inlines.Add(new Run("\n" + STORE_PHONE));
            header.Inlines.Add(new Run("\n" + SEPARATOR_LINE));

            document.Blocks.Add(header);
        }

        private void AddOrderInfo(FlowDocument document, OrderDTO order, bool isReprint = false)
        {
            var info = new Paragraph
            {
                Margin = new Thickness(0, 5, 0, 5)
            };

            info.Inlines.Add(new Run($"주문번호: {order.OrderNumber}\n"));
            info.Inlines.Add(new Run($"테이블: {order.TableName}\n"));
            info.Inlines.Add(new Run($"주문일시: {order.OrderDate:yyyy-MM-dd HH:mm:ss}"));
            
            if (isReprint)
            {
                info.Inlines.Add(new Run($"\n재출력시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}") { Foreground = Brushes.Red });
            }

            document.Blocks.Add(info);
            document.Blocks.Add(new Paragraph(new Run(DASH_LINE)));
        }

        private void AddOrderDetails(FlowDocument document, OrderDTO order)
        {
            var header = new Paragraph
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 5)
            };
            header.Inlines.Add(new Run("[주문내역]"));
            document.Blocks.Add(header);

            var table = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 5, 0, 5)
            };

            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(30) });
            table.Columns.Add(new TableColumn { Width = new GridLength(50) });
            table.Columns.Add(new TableColumn { Width = new GridLength(50) });

            var rowGroup = new TableRowGroup();

            foreach (var detail in order.OrderDetails.OrderBy(d => d.MenuItemName))
            {
                var row = new TableRow();

                row.Cells.Add(new TableCell(new Paragraph(new Run(detail.MenuItemName))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Left
                }));

                row.Cells.Add(new TableCell(new Paragraph(new Run(detail.Quantity.ToString()))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                }));

                row.Cells.Add(new TableCell(new Paragraph(new Run(detail.UnitPrice.ToString("N0")))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Right
                }));

                row.Cells.Add(new TableCell(new Paragraph(new Run(detail.SubTotal.ToString("N0")))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Right
                }));

                rowGroup.Rows.Add(row);
            }

            table.RowGroups.Add(rowGroup);
            document.Blocks.Add(table);
            document.Blocks.Add(new Paragraph(new Run(DASH_LINE)));
        }

        private void AddTotalAmount(FlowDocument document, OrderDTO order)
        {
            var total = new Paragraph
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var totalText = $"합계:{new string(' ', 20)}";
            var amount = $"{order.TotalAmount.ToString("N0")}원";
            var spacing = 35 - totalText.Length - amount.Length;
            if (spacing > 0) totalText += new string(' ', spacing);

            total.Inlines.Add(new Run(totalText + amount));
            document.Blocks.Add(total);
            document.Blocks.Add(new Paragraph(new Run(DASH_LINE)));
        }

        private void AddPaymentInfo(FlowDocument document, OrderDTO order)
        {
            var payment = new Paragraph
            {
                Margin = new Thickness(0, 5, 0, 5)
            };

            payment.Inlines.Add(new Run($"결제방법: {order.PaymentMethod ?? "미정"}\n"));
            if (order.PaymentDate.HasValue)
            {
                payment.Inlines.Add(new Run($"결제일시: {order.PaymentDate.Value:yyyy-MM-dd HH:mm:ss}"));
            }

            document.Blocks.Add(payment);
        }

        private void AddMultiPaymentInfo(FlowDocument document, List<PaymentTransactionDTO> paymentTransactions)
        {
            var payment = new Paragraph
            {
                Margin = new Thickness(0, 5, 0, 5),
                FontWeight = FontWeights.Bold
            };

            payment.Inlines.Add(new Run("결제 내역\n"));
            document.Blocks.Add(payment);

            foreach (var transaction in paymentTransactions.Where(t => t.Status == "Completed"))
            {
                var transInfo = new Paragraph
                {
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var method = transaction.DisplayPaymentMethod;
                var amount = transaction.Amount.ToString("N0");
                var date = transaction.PaymentDate.ToString("MM/dd HH:mm");
                
                transInfo.Inlines.Add(new Run($"{method}: {amount}원 ({date})"));
                document.Blocks.Add(transInfo);
            }

            // 취소된 결제가 있는 경우
            var cancelledTransactions = paymentTransactions.Where(t => t.Status == "Cancelled").ToList();
            if (cancelledTransactions.Any())
            {
                document.Blocks.Add(new Paragraph(new Run("\n취소된 결제"))
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Red
                });

                foreach (var transaction in cancelledTransactions)
                {
                    var transInfo = new Paragraph
                    {
                        Margin = new Thickness(0, 2, 0, 2),
                        Foreground = Brushes.Red
                    };

                    var method = transaction.DisplayPaymentMethod;
                    var amount = transaction.Amount.ToString("N0");
                    var date = transaction.CancelledDate?.ToString("MM/dd HH:mm") ?? "";
                    
                    transInfo.Inlines.Add(new Run($"{method}: {amount}원 (취소: {date})"));
                    document.Blocks.Add(transInfo);
                }
            }
        }

        private void AddFooter(FlowDocument document, bool isReprint = false)
        {
            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            footer.Inlines.Add(new Run(SEPARATOR_LINE + "\n"));
            
            if (isReprint)
            {
                footer.Inlines.Add(new Run("[ 재출력 영수증 ]\n") { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
            }
            
            footer.Inlines.Add(new Run("감사합니다") { FontSize = 14 });
            footer.Inlines.Add(new Run("\n" + SEPARATOR_LINE));

            document.Blocks.Add(footer);
        }

        public async Task<bool> PrintKitchenOrderAsync(OrderDTO order, IEnumerable<OrderDetailDTO> newItems)
        {
            try
            {
                // UI 스레드에서 실행되어야 함
                var tcs = new TaskCompletionSource<bool>();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var document = CreateKitchenOrderDocument(order, newItems);
                        var printDialog = new PrintDialog();

                        // 주방 프린터용 용지 크기 설정 (80mm x 297mm)
                        printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                            80 * 96 / 25.4,  // 80mm를 인치로 변환 후 DPI(96) 적용
                            297 * 96 / 25.4); // 297mm를 인치로 변환 후 DPI(96) 적용

                        // 주방 프린터는 다이얼로그 없이 바로 출력
                        printDialog.PrintDocument(
                            ((IDocumentPaginatorSource)document).DocumentPaginator,
                            $"주방주문서 - {order.OrderNumber}");
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"주방 프린터 출력 오류: {ex.Message}");
                        tcs.SetException(ex);
                    }
                });
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"주방 프린터 출력 오류: {ex.Message}");
                return false;
            }
        }

        private FlowDocument CreateKitchenOrderDocument(OrderDTO order, IEnumerable<OrderDetailDTO> newItems)
        {
            var document = new FlowDocument
            {
                PageWidth = 280,  // 80mm 영수증 너비에 맞춤
                PageHeight = 600, // 적절한 높이 설정
                PagePadding = new Thickness(10, 10, 10, 10),
                ColumnWidth = 260,
                FontFamily = new FontFamily("Consolas, 맑은 고딕"),
                FontSize = 14      // 주방에서 읽기 쉽도록 크게 설정
            };

            AddKitchenHeader(document);
            AddKitchenOrderInfo(document, order);
            AddKitchenOrderItems(document, newItems);
            AddKitchenFooter(document);

            return document;
        }

        private void AddKitchenHeader(FlowDocument document)
        {
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            header.Inlines.Add(new Run(SEPARATOR_LINE + "\n"));
            header.Inlines.Add(new Run("[ 주방 주문서 ]") { FontSize = 18, FontWeight = FontWeights.Bold });
            header.Inlines.Add(new Run("\n" + SEPARATOR_LINE));

            document.Blocks.Add(header);
        }

        private void AddKitchenOrderInfo(FlowDocument document, OrderDTO order)
        {
            var info = new Paragraph
            {
                Margin = new Thickness(0, 5, 0, 5),
                FontSize = 12
            };

            info.Inlines.Add(new Run($"주문번호: {order.OrderNumber}\n"));
            info.Inlines.Add(new Run($"테이블: {order.TableName}\n"));
            info.Inlines.Add(new Run($"시간: {DateTime.Now:HH:mm:ss}"));

            document.Blocks.Add(info);
            document.Blocks.Add(new Paragraph(new Run(DASH_LINE)));
        }

        private void AddKitchenOrderItems(FlowDocument document, IEnumerable<OrderDetailDTO> newItems)
        {
            foreach (var item in newItems.OrderBy(i => i.MenuItemName))
            {
                var itemParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 5, 0, 5),
                    FontSize = 16,  // 메뉴명은 크게
                    FontWeight = FontWeights.Bold
                };

                // 메뉴명과 수량을 한 줄에 표시
                var menuName = item.MenuItemName;
                var quantity = $" x {item.Quantity}";
                
                // 메뉴명 길이 제한 (너무 길면 줄바꿈)
                if (menuName.Length > 20)
                {
                    menuName = menuName.Substring(0, 20) + "...";
                }

                // 왼쪽 정렬된 메뉴명
                itemParagraph.Inlines.Add(new Run(menuName));
                
                // 오른쪽 정렬을 위한 공백 추가
                var totalLength = 25; // 전체 길이
                var spacingLength = totalLength - menuName.Length - quantity.Length;
                if (spacingLength > 0)
                {
                    itemParagraph.Inlines.Add(new Run(new string(' ', spacingLength)));
                }
                
                // 수량
                itemParagraph.Inlines.Add(new Run(quantity) { FontSize = 18 });

                document.Blocks.Add(itemParagraph);
            }
        }

        private void AddKitchenFooter(FlowDocument document)
        {
            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            footer.Inlines.Add(new Run("\n" + SEPARATOR_LINE));

            document.Blocks.Add(footer);
        }
    }
}